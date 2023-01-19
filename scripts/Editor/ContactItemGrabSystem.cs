using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using System.IO;
using System;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VRC.SDK3.Dynamics.Contact.Components;

namespace sophia.PickupAndWeaponSystem.Editor {
    public class ContactItemGrabSystem : EditorWindow {
        private string _itemName;

        private readonly string _animationSavePath = "Assets/sophia's pickups and weapon system/Generated/";
        private string AnimationsFolderPath => _animationSavePath + _itemName + "/";

        private AnimatorController _fxAnimator;
        private VRCAvatarDescriptor _avatar;
        private GameObject _worldConstraint;
        private GameObject _containerObject;
        private GameObject _itemPrefab;
        private GameObject _trackingPrefab;
        private GameObject _cullPrefab;
        private GameObject _targetPrefab;
        AnimatorController _copyFromController;

        private string _animationRetargetPath;

        private Dictionary<string, bool> _selectedLayers = new Dictionary<string, bool>();

        private bool _worldConstraintExpanded;
        private bool _cullObjectExpanded;
        private bool _defaultObjectsFoldoutExpanded;
        private bool _creditsExpanded;
        private Vector2 _scrollPosition;

        // Item Names
        private string CullObjectName => "Cull";
        private string ObjectContainerName => _itemName;
        private string TargetObjectName => _itemName + " Target";
        private string TrackingObjectName => _itemName + " Tracking";
        private string ItemObjectName => _itemName;

        [MenuItem("Tools/Sophia/Item Pickup System Setup Tool")]
        static void Init() {
            ContactItemGrabSystem window = (ContactItemGrabSystem)GetWindow(typeof(ContactItemGrabSystem), false, "Item Pickup System Setup Tool");
            window.Show();
        }

        void OnDisable() { }

        void OnEnable() { }

        void OnGUI() {
            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label")) {
                alignment = TextAnchor.UpperCenter,
                fontSize = 22
            };

            var centeredStyleSmall = new GUIStyle(centeredStyle) {
                fontSize = 10
            };

            GUILayout.Label("Item Pickup System", centeredStyle);
            GUILayout.Label("by Sophia, script by Tayou & contributors", centeredStyleSmall);

            _itemName = EditorGUILayout.TextField("Item Name", _itemName);
            Directory.CreateDirectory(_animationSavePath);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            VRCAvatarDescriptor newAvatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(VRCAvatarDescriptor), true, new GUILayoutOption[] { });
            if (newAvatar != null && newAvatar != _avatar) {
                _avatar = newAvatar;
                _fxAnimator = (AnimatorController)_avatar.baseAnimationLayers[4].animatorController;
            }

#region World Constraint
            int worldConstraintFound;
            if (_worldConstraint == null) {
                // No world constraint
                worldConstraintFound = 1;
                _worldConstraintExpanded = true;
            } else if (!_avatar.transform.Find(_worldConstraint.name)) {
                //its not in Avatar
                worldConstraintFound = 2;
                _worldConstraintExpanded = true;
            } else {
                worldConstraintFound = 0;
            }

            _worldConstraintExpanded = EditorGUILayout.Foldout(_worldConstraintExpanded, "World Constraint");
            if (_worldConstraintExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(new GUIContent(worldConstraintFound == 2
                    ? "Prefab not found in Avatar, Press button below to Instantiate on Avatar"
                    : "Assign this to Either a Prefab for the world Constraint or a existing World Constraint under your Avatar hierarchy"));
                _worldConstraint = (GameObject)EditorGUILayout.ObjectField("World Constraint", _worldConstraint, typeof(GameObject), true, new GUILayoutOption[] { });
                if (GUILayout.Button("Set up World Constraint!")) {
                    PlaceWorldConstraint();
                }
                EditorGUI.indentLevel--;
            }
            GUI.enabled = worldConstraintFound == 0;
#endregion

#region Cull Object
            _cullObjectExpanded = EditorGUILayout.Foldout(_cullObjectExpanded, "Cull Object");
            if (_cullObjectExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(new GUIContent("This Prefab will make sure the item will never be culled, preventing desync for the item. Press button below to Instantiate on Avatar"));
                _cullPrefab = (GameObject)EditorGUILayout.ObjectField("Cull Object", _cullPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
                if (GUILayout.Button("Set up Cull Object!")) {
                    PlaceCullObject();
                }
                EditorGUI.indentLevel--;
            }
#endregion

            _itemPrefab = (GameObject)EditorGUILayout.ObjectField("Item Prefab", _itemPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
            _trackingPrefab = (GameObject)EditorGUILayout.ObjectField("Tracking Prefab", _trackingPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
            _targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Object", _targetPrefab, typeof(GameObject), true, new GUILayoutOption[] { });

            if (GUILayout.Button("Set up GameObjects!")) {
                FindAndPlacePrefabs();
            }

            _fxAnimator = (AnimatorController)EditorGUILayout.ObjectField("FX Controller", _fxAnimator, typeof(AnimatorController), true, new GUILayoutOption[] { });
            _copyFromController = (AnimatorController)EditorGUILayout.ObjectField("Animator to copy from", _copyFromController, typeof(AnimatorController), true, new GUILayoutOption[] { });
            if (GUILayout.Button("Set up Animator!")) {
                AdjustControllerLayers();
            }

            // TODO: get these prefabs & animator automatically somehow, probably by hardcoding the asset IDs and using AssetDatabase.Find()
            /*
#region Automatically Gathered Objects
            defaultObjectsFoldoutExpanded = EditorGUILayout.Foldout(defaultObjectsFoldoutExpanded, "Built In Assets");
            if (defaultObjectsFoldoutExpanded) {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("World Prefab", null, typeof(GameObject), true, new GUILayoutOption[] { });
                EditorGUILayout.HelpBox(new GUIContent("This Prefab should be a empty object at 0, 0, 0 with no rotation and default scale. it is used to fix objects on the avatar in world space."));
                EditorGUILayout.ObjectField("Cull Prefab", null, typeof(GameObject), true, new GUILayoutOption[] { });
                EditorGUILayout.HelpBox(new GUIContent("This Prefab is a invisible, very large box, that will prevent the avatar from being culled when Props are spawned in. This is necessary to ensure the Props position doesn't get lost for remote players."));
                EditorGUILayout.ObjectField("---", null, typeof(GameObject), true, new GUILayoutOption[] { });
                EditorGUILayout.HelpBox(new GUIContent("This Prefab should be a empty object at 0, 0, 0 with no rotation and default scale. it is used to fix objects on the avatar in world space."));
            }
#endregion
            */

#region Credits
            GUI.enabled = true;
            _creditsExpanded = EditorGUILayout.Foldout(_creditsExpanded, "Credits");
            if (_creditsExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Sophia: Initial System: Animations, Animator Controller, Prefabs");
                EditorGUILayout.LabelField("Tayou: Setup Script");
                EditorGUILayout.LabelField("AirGamer: LayerCopy Script, used in Setup Script");

                // Add your name here if you contributed to the script or prefab!

                EditorGUI.indentLevel--;
            }
#endregion

            EditorGUILayout.EndScrollView();
        }

        void AdjustControllerLayers() {
            if (!_copyFromController || !_fxAnimator) return;

            _selectedLayers = new Dictionary<string, bool>();
            bool firstLayer = false;
            foreach (var layer in _copyFromController.layers) {
                _selectedLayers.Add(layer.name, firstLayer);
                firstLayer = true;
            }
            //, new int[]{1, 2, 3}, animationSavePath, ("SophiaItemSys", "SophiaItemSys/" + itemName)
            Copy(_copyFromController, _fxAnimator, PreProcessParameter, PostProcessTransitions);
            // TODO: rename layers somewhere here to add itemName
            PrintLog("Layers Pasted");
        }

        /// <summary>
        /// Copies a given Animation Clip from its original Location to a Location made out of the original Path + itemName
        /// </summary>
        /// <param name="sourceMotion"></param>
        /// <returns></returns>
        private Motion CopyMotionAsset(Motion sourceMotion) {
            Debug.Log("Animation Source Path: " + AssetDatabase.GetAssetPath(sourceMotion));

            /*  TODO: this is not ideal as it assumes the source position. Maybe there is some better way, I am not sure...
                Appending the destination folder at the start of the path would work, but would produce quite ugly and long paths.. */
            // in Assets
            string newAnimationClipPath = AssetDatabase.GetAssetPath(sourceMotion).Replace("Assets/sophia's pickups and weapon system/Setup Tool/AnimationAssets/", AnimationsFolderPath);
            // in Packages
            newAnimationClipPath = newAnimationClipPath.Replace("Packages/com.sophia.item-and-weapon-pickup-system/animations", AnimationsFolderPath);
            if (newAnimationClipPath == string.Empty) {
                PrintLog("Motion Copy Failed!\nReturning original");
                return sourceMotion;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(newAnimationClipPath) ?? string.Empty); //this ?? is useless, but fleet wouldn't shut up about possible error...
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceMotion), newAnimationClipPath)) {
                Debug.LogWarning("Copy Failed");
                return sourceMotion;
            }

            Motion copiedMotion = (Motion)AssetDatabase.LoadAssetAtPath(newAnimationClipPath, typeof(Motion));
            if (copiedMotion == null) {
                PrintLog("Loading Copied Asset from " + newAnimationClipPath + " was not successful, Abort!");
                return sourceMotion;
            }

            PrintLog("Copied Animation from " + AssetDatabase.GetAssetPath(sourceMotion) + " to " + newAnimationClipPath);
            return copiedMotion;
        }

        /// <summary>
        /// Retargets animation paths, following the AnimationUtility.CalculateTransformPath() of the various objects inside the main object
        /// </summary>
        /// <param name="animationClip"></param>
        /// <returns></returns>
        private AnimationClip UpdateAnimationClipPath(AnimationClip animationClip) {
            if (animationClip == null) {
                Debug.LogWarning("Animation Clip is null (empty)");
                return null;
            }

            // Binding List.
            EditorCurveBinding[] editorCuriveBinding = AnimationUtility.GetCurveBindings(animationClip);
            foreach (var editorCurveBinding in editorCuriveBinding) {
                EditorGUILayout.BeginHorizontal();
                // rotation can not be changed like this, TODO: figure out how to avoid the errors in console. It seems even though it "doesn't work" it still does it?
                if (editorCurveBinding.path.Contains("localEulerAnglesRaw")) {
                    continue;
                }

                string newPath = editorCurveBinding.path;
                if (editorCurveBinding.path.Contains("sophia's hand tracker/Pickup holder/Item 1")) {
                    newPath = editorCurveBinding.path.Replace("sophia's hand tracker/Pickup holder/Item 1", AnimationUtility.CalculateTransformPath(_itemPrefab.transform, _avatar.transform));
                } else if (editorCurveBinding.path.Contains("sophia's hand tracker/Cull")) {
                    newPath = editorCurveBinding.path.Replace("sophia's hand tracker/Cull", AnimationUtility.CalculateTransformPath(_cullPrefab.transform, _avatar.transform));
                } else if (editorCurveBinding.path.Contains("sophia's hand tracker/World/item 1 tracking")) {
                    newPath = editorCurveBinding.path.Replace("sophia's hand tracker/World/item 1 tracking", AnimationUtility.CalculateTransformPath(_trackingPrefab.transform, _avatar.transform));
                }

                animationClip.SetCurve(newPath  , editorCurveBinding.type, editorCurveBinding.propertyName, AnimationUtility.GetEditorCurve(animationClip, editorCurveBinding));
                animationClip.SetCurve(editorCurveBinding.path, editorCurveBinding.type, editorCurveBinding.propertyName, null);
            }
            return animationClip;
        }

        /// <summary>
        /// Ratargets Given Constraint to new Transform
        /// </summary>
        /// <param name="constraint">Constraint to Modify</param>
        /// <param name="target">Target GameObject to constrain to</param>
        /// <param name="index">Index Position in Constraint Sources</param>
        void RetargetConstraint(ParentConstraint constraint, GameObject target, int index) {
            ConstraintSource owo = constraint.GetSource(index);
            owo.sourceTransform = target.transform;
            constraint.SetSource(0, owo);

            PrintLog("Constraint Retargetted");
        }


        /// <summary>
        /// Place World Constraint if not placed already
        /// </summary>
        void PlaceWorldConstraint() {
            if (!_worldConstraint.transform.IsChildOf(_avatar.transform)) {
                _worldConstraint = Instantiate(_worldConstraint, _avatar.transform);
            }
        }

        /// <summary>
        /// Place Cull Object, which prevents the avatar from being culled, in order to prevent the item from desyncing
        /// </summary>
        void PlaceCullObject() {
            if (!_cullPrefab.transform.IsChildOf(_worldConstraint.transform)) {
                _cullPrefab = Instantiate(_cullPrefab, _worldConstraint.transform);
                _cullPrefab.name = CullObjectName;
            }
        }

        /// <summary>
        /// Places prefabs into one common parent for easy organization
        /// TODO: maybe use single prefab for instantiation, makes it harder to mess up references between the various prefabs
        /// -> WorldPrefab
        ///     -> Cull
        ///     -> "itemName"
        ///         -> Tracking
        ///         -> Target
        ///         -> Item
        /// </summary>
        void FindAndPlacePrefabs() {
            PlaceWorldConstraint();
            PlaceCullObject();

            // Place Empty Parent Object
            if ((object)_containerObject == null) {
                _containerObject = new GameObject(ObjectContainerName) {
                    transform = {
                        parent = _worldConstraint.transform,
                        localPosition = Vector3.zero
                    }
                };
            } else if (_containerObject.name != ObjectContainerName) {
                _containerObject.name = ObjectContainerName;
            }

            #region item specific
            {
                // Place Target Prefab
                if (!_targetPrefab.transform.IsChildOf(_worldConstraint.transform)) {
                    _targetPrefab = Instantiate(_targetPrefab, _worldConstraint.transform);
                    _targetPrefab.name = TargetObjectName;
                } else if (_targetPrefab.name != TargetObjectName) {
                    _targetPrefab.name = TargetObjectName;
                }

                // Place Tracking Prefab
                if (!_trackingPrefab.transform.IsChildOf(_worldConstraint.transform)) {
                    _trackingPrefab = Instantiate(_trackingPrefab, _worldConstraint.transform);
                    _trackingPrefab.name = TrackingObjectName;
                } else if (_trackingPrefab.name != TrackingObjectName) {
                    _trackingPrefab.name = TrackingObjectName;
                }

                // Place Item Prefab
                if (!_itemPrefab.transform.IsChildOf(_worldConstraint.transform)) {
                    _itemPrefab = Instantiate(_itemPrefab, _worldConstraint.transform);
                    _itemPrefab.name = ItemObjectName;
                } else if (_itemPrefab.name != ItemObjectName) {
                    _itemPrefab.name = ItemObjectName;
                }
            }
            #endregion

            PrintLog("Prefabs Placed");

            RetargetConstraint(_trackingPrefab.transform.Find("object").GetComponent<ParentConstraint>(), _targetPrefab, 0);
            UpdateContactReceivers(_containerObject.transform);
        }

        void UpdateContactReceivers(Transform searchRoot) {
            //This is very slow, which doesn't matter much in the editor, but if there is a better way, we should use that
            VRCContactReceiver[] contactReceivers = Resources.FindObjectsOfTypeAll<VRCContactReceiver>();
            foreach (var item in contactReceivers) {
                if (item.transform.IsChildOf(searchRoot)) {
                    item.parameter = item.parameter.Replace("SophiaItemSys", "SophiaItemSys/" + _itemName);
                }
            }
        }

        private void PrintLog(string text) {
            Debug.Log("<color=#BB22FF>Item Pickup System Setup Tool</color>: " + text);
        }

        private AnimatorControllerParameter PreProcessParameter(AnimatorControllerParameter parameter) {
            if (parameter.name.Contains("SophiaItemSys")) {
                return new AnimatorControllerParameter() {
                    name = parameter.name.Replace("SophiaItemSys", "SophiaItemSys/" + _itemName), type = parameter.type,
                    defaultBool = parameter.defaultBool, defaultFloat = parameter.defaultFloat,
                    defaultInt = parameter.defaultInt
                };
            }

            return null;
        }

        private void PostProcessTransitions(AnimatorTransitionBase[] transitions, Func<AnimatorState, AnimatorTransitionBase> newTransition, Action<AnimatorTransitionBase> removeTransition) {
            foreach (AnimatorTransitionBase tranistion in transitions) {
                foreach (AnimatorCondition condition in tranistion.conditions) {
                    if (condition.parameter.Contains("SophiaItemSys")) {
                        tranistion.RemoveCondition(condition);
                        tranistion.AddCondition(condition.mode, condition.threshold, condition.parameter.Replace("SophiaItemSys", "SophiaItemSys/" + _itemName));
                    }
                }
            }
        }

        /// <summary>
        /// Copies selected layers from parSrcAnimator to parDstAnimator.
        /// </summary>
        /// <param name="parSrcAnimator">Source animator controller</param>
        /// <param name="parDstAnimator">Destination animator controller</param>
        /// <param name="parameterPreProcessor"> Can be null.
        /// Method to inspect a source parameter and returns a new, modified parameter to be inserted, input parameter should not be modified.
        /// Returns null to insert a copy of the source parameter as is.
        /// Method signature "AnimatorControllerParameter FuncName(AnimatorControllerParameter parameter)"
        /// </param>
        /// <param name="transitionPostProcessor"> Can be null.
        /// Method to modify, add or remove state transitions as required.
        /// Method signature "void FuncName(AnimatorTransitionBase[] transitions, Func<AnimatorState, AnimatorTransitionBase> newTransition, Action<AnimatorTransitionBase> removeTransition)"
        /// </param>
        private void Copy(AnimatorController parSrcAnimator, AnimatorController parDstAnimator,
            Func<AnimatorControllerParameter, AnimatorControllerParameter> parameterPreProcessor,
            Action<AnimatorTransitionBase[], Func<AnimatorState, AnimatorTransitionBase>, Action<AnimatorTransitionBase>> transitionPostProcessor) {
            if (parSrcAnimator == null | parDstAnimator == null)
                return;

            List<string> usedParameterNames = new List<string>();
            List<string> usedLayerNames = new List<string>();

            //Find Used Params
            foreach (AnimatorControllerLayer layer in parSrcAnimator.layers) {
                if (!_selectedLayers[layer.name])
                    continue;

                CollectParameters(usedParameterNames, usedLayerNames, layer.stateMachine);

                //Also check for synced layers
                if (layer.syncedLayerIndex != -1)
                    usedLayerNames.Add(parSrcAnimator.layers[layer.syncedLayerIndex].name);
            }

            //Check params
            List<AnimatorControllerParameter> neededParameters = new List<AnimatorControllerParameter>();
            Dictionary<string, string> renamedParameterNames = new Dictionary<string, string>();
            foreach (string paramName in usedParameterNames) {
                //Find Param in src
                AnimatorControllerParameter srcParam = null;
                foreach (AnimatorControllerParameter p in parSrcAnimator.parameters) {
                    if (p.name == paramName) {
                        srcParam = p;
                        break;
                    }
                }

                if (srcParam == null) {
                    Debug.LogWarning("Used Parameter \"" + paramName + "\" not found in Source Animator!?!");
                    continue;
                }

                //Find Param in dst
                AnimatorControllerParameter dstParam = null;
                foreach (AnimatorControllerParameter p in parDstAnimator.parameters) {
                    if (p.name == paramName) {
                        dstParam = p;
                        break;
                    }
                }

                //Preprocess
                if (parameterPreProcessor != null) {
                    AnimatorControllerParameter processedParam = parameterPreProcessor(srcParam);
                    if (processedParam != null) {
                        //Was param renamed?
                        if (processedParam.name != srcParam.name)
                            renamedParameterNames.Add(srcParam.name, processedParam.name);
                        srcParam = processedParam;
                    }
                }

                //Check DstParam
                if (dstParam == null)
                    neededParameters.Add(srcParam);
                else if (dstParam.type == srcParam.type) {
                    //Check Default values, log if different
                    switch (dstParam.type) {
                        case AnimatorControllerParameterType.Trigger:
                        case AnimatorControllerParameterType.Bool:
                            if (dstParam.defaultBool != srcParam.defaultBool)
                                Debug.LogWarning($"Paramter \"{paramName}\" has differing default values, using destination value");
                            break;
                        case AnimatorControllerParameterType.Int:
                            if (dstParam.defaultInt != srcParam.defaultInt)
                                Debug.LogWarning($"Paramter \"{paramName}\" has differing default values, using destination value");
                            break;
                        case AnimatorControllerParameterType.Float:
                            if (dstParam.defaultFloat != srcParam.defaultFloat)
                                Debug.LogWarning($"Paramter \"{paramName}\" has differing default values, using destination value");
                            break;
                        default:
                            break;
                    }
                } else {
                    Debug.LogError($"Parameter \"{paramName}\" exists in destination animator, but with different type");
                    return;
                }
            }

            //Check layers
            foreach (string layerName in usedLayerNames) {
                if (!_selectedLayers[layerName]) {
                    Debug.LogError("A layer is required, but not selected (target of synced layer?)");
                    return;
                }
            }

            //Copy Params
            foreach (AnimatorControllerParameter param in neededParameters) {
                AnimatorControllerParameter nParam = new AnimatorControllerParameter {
                    name = param.name,
                    type = param.type
                };

                switch (param.type) {
                    case AnimatorControllerParameterType.Trigger:
                    case AnimatorControllerParameterType.Bool:
                        nParam.defaultBool = param.defaultBool;
                        break;
                    case AnimatorControllerParameterType.Int:
                        nParam.defaultInt = param.defaultInt;
                        break;
                    case AnimatorControllerParameterType.Float:
                        nParam.defaultFloat = param.defaultFloat;
                        break;
                    default:
                        break;
                }

                parDstAnimator.AddParameter(nParam);
            }

            //Note that the layers class is recreated in AnimatorController.layers, and thus won't be equal to a second instance gotten from the second array
            Dictionary<AnimatorControllerLayer, AnimatorControllerLayer> layerMapping = new Dictionary<AnimatorControllerLayer, AnimatorControllerLayer>();

            //Copy Layer
            AnimatorControllerLayer[] srcLayers = parSrcAnimator.layers;
            List<AnimatorControllerLayer> newLayers = new List<AnimatorControllerLayer>();
            foreach (AnimatorControllerLayer layer in srcLayers) {
                if (!_selectedLayers[layer.name])
                    continue;

                AnimatorControllerLayer newLayer = new AnimatorControllerLayer {
                    avatarMask = layer.avatarMask,
                    blendingMode = layer.blendingMode,
                    defaultWeight = srcLayers[0] == layer ? 1f : layer.defaultWeight,
                    iKPass = layer.iKPass,
                    name = parDstAnimator.MakeUniqueLayerName(layer.name),
                    stateMachine = new AnimatorStateMachine {
                        name = parDstAnimator.MakeUniqueLayerName(layer.name)
                    }
                };
                AnimatorStateMachine newStateMachine = newLayer.stateMachine;

                AssetDatabase.AddObjectToAsset(newStateMachine, AssetDatabase.GetAssetPath(parDstAnimator));
                newStateMachine.hideFlags = HideFlags.HideInHierarchy;

                DeepCopy(layer.stateMachine, newStateMachine, renamedParameterNames, transitionPostProcessor);

                newLayers.Add(newLayer);
                layerMapping.Add(layer, newLayer);
            }

            //Setup Synced Layers
            foreach (AnimatorControllerLayer layer in srcLayers) {
                if (!_selectedLayers[layer.name])
                    continue;

                if (layer.syncedLayerIndex == -1)
                    continue;

                AnimatorControllerLayer nLayer = layerMapping[layer];

                AnimatorControllerLayer oLinkedLayer = srcLayers[layer.syncedLayerIndex];
                AnimatorControllerLayer nLinkedLayer = layerMapping[oLinkedLayer];

                //Compute new Index
                int index = newLayers.IndexOf(nLinkedLayer) + srcLayers.Length;
                //Set synced properties
                nLayer.syncedLayerIndex = index;
                nLayer.syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;

                //Set Layer overrides
                Dictionary<AnimatorState, AnimatorState> stateMapping = new Dictionary<AnimatorState, AnimatorState>();
                BuildStateMapping(stateMapping, oLinkedLayer.stateMachine, nLinkedLayer.stateMachine);

                foreach (KeyValuePair<AnimatorState, AnimatorState> kv in stateMapping) {
                    Motion oMotion = layer.GetOverrideMotion(kv.Key);
                    Motion nMotion = DeepCopyMotion(oLinkedLayer.stateMachine, nLinkedLayer.stateMachine, oMotion, renamedParameterNames);

                    nLayer.SetOverrideMotion(kv.Value, nMotion);

                    //TODO: behaviours
                    nLayer.SetOverrideBehaviours(kv.Value, null);
                }
            }

            //Add layers
            foreach (AnimatorControllerLayer layer in newLayers)
                parDstAnimator.AddLayer(layer);

            EditorUtility.SetDirty(parDstAnimator);
            AssetDatabase.SaveAssets();
            //Hope it works
        }

        private void CollectParameters(List<string> paramList, List<string> layerList, AnimatorStateMachine stateMachine) {
            //AnyState Transitions
            InspectTransitions(paramList, stateMachine.anyStateTransitions);

            //Entry Transitions
            InspectTransitions(paramList, stateMachine.entryTransitions);

            //Behaviours
            foreach (StateMachineBehaviour behaviour in stateMachine.behaviours)
                InspectStateBehaviour(paramList, layerList, behaviour);

            //states
            foreach (ChildAnimatorState state in stateMachine.states) {
                //State Parameters
                if (state.state.cycleOffsetParameterActive)
                    if (!paramList.Contains(state.state.cycleOffsetParameter))
                        paramList.Add(state.state.cycleOffsetParameter);

                if (state.state.mirrorParameterActive)
                    if (!paramList.Contains(state.state.mirrorParameter))
                        paramList.Add(state.state.mirrorParameter);

                if (state.state.speedParameterActive)
                    if (!paramList.Contains(state.state.speedParameter))
                        paramList.Add(state.state.speedParameter);

                if (state.state.timeParameterActive)
                    if (!paramList.Contains(state.state.timeParameter))
                        paramList.Add(state.state.timeParameter);

                foreach (StateMachineBehaviour behaviour in state.state.behaviours)
                    InspectStateBehaviour(paramList, layerList, behaviour);

                //Blend Trees
                InspectMotion(paramList, state.state.motion);

                //Trainsitions
                InspectTransitions(paramList, state.state.transitions);
            }

            //Child StateMachines
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines) {
                CollectParameters(paramList, layerList, childStateMachine.stateMachine);

                //Transitions
                InspectTransitions(paramList, stateMachine.GetStateMachineTransitions(childStateMachine.stateMachine));
            }

            if (paramList.Contains("ContactTracker")) {
                Debug.Log("Its here!!!" + paramList.IndexOf("ContactTracker"));
            }
        }

        private void InspectTransitions(List<string> paramList, AnimatorTransitionBase[] transitions) {
            foreach (AnimatorTransitionBase transition in transitions) {
                foreach (AnimatorCondition condition in transition.conditions) {
                    if (!paramList.Contains(condition.parameter))
                        paramList.Add(condition.parameter);
                }
            }
        }

        private void InspectMotion(List<string> paramList, Motion motion) {
            if (motion is BlendTree blendTree)
                InspectBlendTree(paramList, blendTree);
        }

        private void InspectBlendTree(List<string> paramList, BlendTree tree) {
            switch (tree.blendType) {
                case BlendTreeType.Direct:
                    foreach (var child in tree.children) {
                        if (!paramList.Contains(child.directBlendParameter)) {
                            paramList.Add(child.directBlendParameter);
                        }
                    }

                    break;
                case BlendTreeType.Simple1D:
                    if (!paramList.Contains(tree.blendParameter))
                        paramList.Add(tree.blendParameter);
                    break;
                case BlendTreeType.FreeformCartesian2D:
                case BlendTreeType.FreeformDirectional2D:
                case BlendTreeType.SimpleDirectional2D:
                    if (!paramList.Contains(tree.blendParameter))
                        paramList.Add(tree.blendParameter);

                    if (!paramList.Contains(tree.blendParameterY))
                        paramList.Add(tree.blendParameterY);

                    break;
            }

            foreach (var child in tree.children)
                InspectMotion(paramList, child.motion);
        }

        private void InspectStateBehaviour(List<string> paramList, List<string> layerList, StateMachineBehaviour behaviour) {
            switch (behaviour) {
#if VRC_SDK_VRCSDK3
                case VRC_AnimatorLayerControl animLayerControl:
                    //This can set layers outside of the src animatior
                    //so ignore and log a warning
                    Debug.LogWarning("VRCAnimatorLayerControl Found, If it controlling a layer from the source animatior, then the Layer index will need to be updated manually");
                    break;
                case VRC_AnimatorLocomotionControl locControl:
                case VRC_AnimatorTemporaryPoseSpace tempPoseSpace:
                case VRC_AnimatorTrackingControl trackControl:
                    break;
                case VRC_AvatarParameterDriver paramDriver:
                    foreach (var param in paramDriver.parameters) {
                        if (!paramList.Contains(param.name))
                            paramList.Add(param.name);
                        if (param.type == VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Copy) {
                            if (!paramList.Contains(param.name))
                                paramList.Add(param.name);
                        }
                    }
                    break;
                case VRC_PlayableLayerControl playLayerControl:
                    break;
#endif
                default:
                    Debug.LogWarning("Unkown StateMachineBehaviour Found");
                    break;
            }
        }

        private void DeepCopy(AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine, Dictionary<string, string> renamedParameters,
            Action<AnimatorTransitionBase[], Func<AnimatorState, AnimatorTransitionBase>, Action<AnimatorTransitionBase>> transitionPostProcessor) {
            Dictionary<AnimatorState, AnimatorState> stateMapping = new Dictionary<AnimatorState, AnimatorState>();
            Dictionary<AnimatorStateMachine, AnimatorStateMachine> machineMapping = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();

            //Position Default nodes
            dstStateMachine.entryPosition = srcStateMachine.entryPosition;
            dstStateMachine.anyStatePosition = srcStateMachine.anyStatePosition;
            dstStateMachine.exitPosition = srcStateMachine.exitPosition;
            dstStateMachine.parentStateMachinePosition = srcStateMachine.parentStateMachinePosition;

            //Copy Behaviours
            foreach (StateMachineBehaviour oBehaviour in srcStateMachine.behaviours) {
                StateMachineBehaviour nBehaviour = dstStateMachine.AddStateMachineBehaviour(oBehaviour.GetType());
                CopyStateBehaviour(oBehaviour, nBehaviour, renamedParameters);
            }

            //Copy States
            foreach (ChildAnimatorState state in srcStateMachine.states) {
                AnimatorState nState = dstStateMachine.AddState(state.state.name, state.position);
                AnimatorState oState = state.state;

                foreach (StateMachineBehaviour oBehaviour in oState.behaviours) {
                    StateMachineBehaviour nBehaviour = nState.AddStateMachineBehaviour(oBehaviour.GetType());
                    CopyStateBehaviour(oBehaviour, nBehaviour, renamedParameters);
                }

                nState.motion = DeepCopyMotion(srcStateMachine, dstStateMachine, oState.motion, renamedParameters);

                //cycleOffset
                nState.cycleOffset = oState.cycleOffset;
                nState.cycleOffsetParameter = renamedParameters.ContainsKey(oState.cycleOffsetParameter)
                    ? renamedParameters[oState.cycleOffsetParameter]
                    : oState.cycleOffsetParameter;
                nState.cycleOffsetParameterActive = oState.cycleOffsetParameterActive;

                nState.iKOnFeet = oState.iKOnFeet;

                //Mirror
                nState.mirror = oState.mirror;
                nState.mirrorParameter = renamedParameters.ContainsKey(oState.mirrorParameter)
                    ? renamedParameters[oState.mirrorParameter]
                    : oState.mirrorParameter;
                nState.mirrorParameterActive = nState.mirrorParameterActive;

                //Speed
                nState.speed = oState.speed;
                nState.speedParameter = renamedParameters.ContainsKey(oState.speedParameter)
                    ? renamedParameters[oState.speedParameter]
                    : oState.speedParameter;
                nState.speedParameterActive = oState.speedParameterActive;

                nState.tag = oState.tag;

                //Time
                nState.timeParameter = renamedParameters.ContainsKey(oState.timeParameter)
                    ? renamedParameters[oState.timeParameter]
                    : oState.timeParameter;
                nState.timeParameterActive = oState.timeParameterActive;

                nState.writeDefaultValues = oState.writeDefaultValues;

                //Check if default state
                if (srcStateMachine.defaultState == oState)
                    dstStateMachine.defaultState = nState;

                stateMapping.Add(oState, nState);
            }

            //Copy Statemachines
            foreach (ChildAnimatorStateMachine machine in srcStateMachine.stateMachines) {
                AnimatorStateMachine nMachine = dstStateMachine.AddStateMachine(machine.stateMachine.name, machine.position);
                AnimatorStateMachine oMachine = machine.stateMachine;

                DeepCopy(oMachine, nMachine, renamedParameters, transitionPostProcessor);

                machineMapping.Add(oMachine, nMachine);
            }

            //Copy Entry Transitions
            foreach (AnimatorTransition oTransition in srcStateMachine.entryTransitions) {
                AnimatorTransition nTransition = oTransition.destinationState != null
                    ? dstStateMachine.AddEntryTransition(stateMapping[oTransition.destinationState])
                    : dstStateMachine.AddEntryTransition(machineMapping[oTransition.destinationStateMachine]);

                CopyTransition(oTransition, nTransition, renamedParameters);
            }

            //PostProcess
            transitionPostProcessor?.Invoke(srcStateMachine.entryTransitions, dstStateMachine.AddEntryTransition, x => dstStateMachine.RemoveEntryTransition((AnimatorTransition)x));

            //Copy AnyState Transitions
            foreach (AnimatorStateTransition oTransition in srcStateMachine.anyStateTransitions) {
                AnimatorStateTransition nTransition = oTransition.destinationState != null
                    ? dstStateMachine.AddAnyStateTransition(stateMapping[oTransition.destinationState])
                    : dstStateMachine.AddAnyStateTransition(machineMapping[oTransition.destinationStateMachine]);

                CopyStateTransition(oTransition, nTransition, renamedParameters);
            }

            //PostProcess
            transitionPostProcessor?.Invoke(dstStateMachine.anyStateTransitions, dstStateMachine.AddAnyStateTransition, x => dstStateMachine.RemoveAnyStateTransition((AnimatorStateTransition)x));

            //State Transitions
            foreach (ChildAnimatorState state in srcStateMachine.states) {
                AnimatorState nState = stateMapping[state.state];
                AnimatorState oState = state.state;

                foreach (AnimatorStateTransition oTransition in oState.transitions) {
                    AnimatorStateTransition nTransition;
                    if (oTransition.destinationState != null)
                        nTransition = nState.AddTransition(stateMapping[oTransition.destinationState]);
                    else if (oTransition.destinationStateMachine != null)
                        nTransition = nState.AddTransition(machineMapping[oTransition.destinationStateMachine]);
                    else if (oTransition.isExit)
                        nTransition = nState.AddExitTransition();
                    else
                        throw new System.NotSupportedException("Unkown State Transition type");

                    CopyStateTransition(oTransition, nTransition, renamedParameters);
                }

                //PostProcess
                transitionPostProcessor?.Invoke(nState.transitions, target => nState.AddTransition(target), x => nState.RemoveTransition((AnimatorStateTransition)x));
            }

            //StateMachine Transitions
            foreach (ChildAnimatorStateMachine machine in srcStateMachine.stateMachines) {
                AnimatorStateMachine nMachine = machineMapping[machine.stateMachine];
                AnimatorStateMachine oMachine = machine.stateMachine;

                AnimatorTransition[] transitions = srcStateMachine.GetStateMachineTransitions(oMachine);
                foreach (AnimatorTransition oTransition in transitions) {
                    AnimatorTransition nTransition;
                    if (oTransition.destinationState != null)
                        nTransition = dstStateMachine.AddStateMachineTransition(nMachine, stateMapping[oTransition.destinationState]);
                    else if (oTransition.destinationStateMachine != null)
                        nTransition = dstStateMachine.AddStateMachineTransition(nMachine, machineMapping[oTransition.destinationStateMachine]);
                    else if (oTransition.isExit)
                        nTransition = dstStateMachine.AddStateMachineExitTransition(nMachine);
                    else
                        throw new System.NotSupportedException("Unkown StateMachine Transition type");

                    CopyTransition(oTransition, nTransition, renamedParameters);
                }
            }
        }

        //AnimatorTransition adds nothing to AnimatorTransitionBase
        void CopyTransition(AnimatorTransition srcTransition, AnimatorTransition dstTransition, Dictionary<string, string> renamedParameters) => CopyTransitionBase(srcTransition, dstTransition, renamedParameters);

        void CopyStateTransition(AnimatorStateTransition srcTransition, AnimatorStateTransition dstTransition, Dictionary<string, string> renamedParameters) {
            CopyTransitionBase(srcTransition, dstTransition, renamedParameters);

            dstTransition.canTransitionToSelf = srcTransition.canTransitionToSelf;
            dstTransition.duration = srcTransition.duration;
            dstTransition.exitTime = srcTransition.exitTime;
            dstTransition.hasExitTime = srcTransition.hasExitTime;
            dstTransition.hasFixedDuration = srcTransition.hasFixedDuration;
            dstTransition.interruptionSource = srcTransition.interruptionSource;
            //isExit
            //mute
            //name
            dstTransition.offset = srcTransition.offset;
            dstTransition.orderedInterruption = srcTransition.orderedInterruption;
            //Solo
        }

        void CopyTransitionBase(AnimatorTransitionBase srcTransition, AnimatorTransitionBase dstTransition, Dictionary<string, string> renamedParameters) {
            dstTransition.name = srcTransition.name;
            //IsExit
            dstTransition.mute = srcTransition.mute;
            dstTransition.solo = srcTransition.solo;

            foreach (AnimatorCondition condition in srcTransition.conditions) {
                dstTransition.AddCondition(condition.mode, condition.threshold,
                    renamedParameters.ContainsKey(condition.parameter)
                        ? renamedParameters[condition.parameter]
                        : condition.parameter);
            }
        }

        Motion DeepCopyMotion(AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine, Motion motion, Dictionary<string, string> renamedParameters) {
            if (motion == null)
                return null;
            else if (motion is AnimationClip clip)
                return UpdateAnimationClipPath((AnimationClip)CopyMotionAsset(clip));
            else if (motion is BlendTree blendTree) {
                //Is path
                if (AssetDatabase.GetAssetPath(srcStateMachine) == AssetDatabase.GetAssetPath(motion))
                    return DeepCopyBlendTree(srcStateMachine, dstStateMachine, blendTree, renamedParameters);
                else
                    return CopyMotionAsset(blendTree);
            } else {
                Debug.LogError("Unkown Motion Type");
                return null;
            }
        }

        BlendTree DeepCopyBlendTree(AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine, BlendTree tree, Dictionary<string, string> renamedParameters) {
            BlendTree nTree = new BlendTree {
                blendType = tree.blendType,
                name = tree.name,
                maxThreshold = tree.maxThreshold,
                minThreshold = tree.minThreshold,
                useAutomaticThresholds = tree.useAutomaticThresholds,
            };
            if (renamedParameters.ContainsKey(tree.blendParameter))
                nTree.blendParameter = renamedParameters[tree.blendParameter];

            if (renamedParameters.ContainsKey(tree.blendParameterY))
                nTree.blendParameterY = renamedParameters[tree.blendParameterY];

            AssetDatabase.AddObjectToAsset(nTree, dstStateMachine);
            nTree.hideFlags = tree.hideFlags;

            ChildMotion[] motions = tree.children; //returns copy

            for (int i = 0; i < tree.children.Length; i++) {
                motions[i].motion = DeepCopyMotion(srcStateMachine, dstStateMachine, motions[i].motion, renamedParameters);
                if (renamedParameters.ContainsKey(motions[i].directBlendParameter))
                    motions[i].directBlendParameter = renamedParameters[motions[i].directBlendParameter];
            }

            nTree.children = motions;
            return nTree;
        }

        void CopyStateBehaviour(StateMachineBehaviour srcBehaviour, StateMachineBehaviour dstBehaviour, Dictionary<string, string> renamedParameters) {
            System.Type type = srcBehaviour.GetType();

            //Reflection fun, may be slow
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                bool hasHideInInspector = false;
                foreach (var attributeData in field.CustomAttributes)
                    if (attributeData.AttributeType == typeof(HideInInspector))
                        hasHideInInspector = true;

                if (hasHideInInspector) {
                    Debug.Log($"{field.Name} Has Hide In Inspector Attribute, Skipping");
                    return;
                }

                CopyByReflection(field.GetValue(srcBehaviour), (x) => field.SetValue(dstBehaviour, x), field.FieldType);
            }

            //Apply corrections if needed?
            switch (dstBehaviour) {
#if VRC_SDK_VRCSDK3
                case VRC_AnimatorLayerControl animLayerControl:
                case VRC_AnimatorLocomotionControl locControl:
                case VRC_AnimatorTemporaryPoseSpace tempPoseSpace:
                case VRC_AnimatorTrackingControl trackControl:
                case VRC_PlayableLayerControl playLayerControl:
                    break;
                case VRC_AvatarParameterDriver paramDriver:
                    foreach (var param in paramDriver.parameters) {
                        if (renamedParameters.ContainsKey(param.name))
                            param.name = renamedParameters[param.name];
                        if (renamedParameters.ContainsKey(param.source))
                            param.source = renamedParameters[param.source];
                    }
                    break;
#endif
                default:
                    Debug.LogWarning("Unkown StateMachineBehaviour Found");
                    break;
            }
        }

        void CopyByReflection(object srcObject, Action<object> setDstObject, Type type) {
            if (type.IsArray)
                throw new System.NotImplementedException();

            if (type.IsPointer)
                throw new System.NotImplementedException();

            if (type.IsByRef)
                throw new System.NotImplementedException();

            if (type == typeof(string)) {
                setDstObject(srcObject);
                return;
            }

            if (type.IsGenericType) {
                if (type.GetGenericTypeDefinition() == typeof(List<>)) {
                    //Copy the list
                    ConstructorInfo constructor = type.GetConstructor(System.Array.Empty<System.Type>());
                    MethodInfo toArray = type.GetMethod("ToArray");
                    MethodInfo add = type.GetMethod("Add");

                    Type itemType = type.GetGenericArguments()[0];

                    object[] array = (object[])toArray.Invoke(srcObject, null);
                    object newList = constructor.Invoke(null);

                    foreach (object oObject in array)
                        CopyByReflection(oObject, (x) => add.Invoke(newList, new object[] { x }), itemType);

                    setDstObject(newList);
                    return;
                } else
                    throw new System.NotImplementedException();
            }

            if (type.IsClass) {
                if (srcObject == null) {
                    setDstObject(null);
                    return;
                }

                object nObject = System.Activator.CreateInstance(type);
                if (nObject == null)
                    throw new System.NotImplementedException();

                foreach (var objectField in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                    bool hasHideInInspector = false;
                    foreach (var attributeData in objectField.CustomAttributes)
                        if (attributeData.AttributeType == typeof(HideInInspector))
                            hasHideInInspector = true;

                    if (hasHideInInspector) {
                        Debug.Log($"{objectField.Name} Has Hide In Inspector Attribute, Skipping");
                        return;
                    }

                    CopyByReflection(objectField.GetValue(srcObject), (x) => objectField.SetValue(nObject, x), objectField.FieldType);
                }

                setDstObject(nObject);
                return;
            }

            //Assume Value type
            setDstObject(srcObject);
        }

        //Used for Synced Layers, Matches name
        void BuildStateMapping(Dictionary<AnimatorState, AnimatorState> mapping, AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine) {
            foreach (ChildAnimatorState state in srcStateMachine.states) {
                AnimatorState oState = state.state;
                AnimatorState nState = null;
                foreach (ChildAnimatorState state2 in dstStateMachine.states) {
                    if (state.state.name == state2.state.name) {
                        nState = state2.state;
                        break;
                    }
                }

                mapping.Add(oState, nState);
            }

            foreach (ChildAnimatorStateMachine machine in srcStateMachine.stateMachines) {
                AnimatorStateMachine oMachine = machine.stateMachine;
                AnimatorStateMachine nMachine = null;
                foreach (ChildAnimatorStateMachine machine2 in dstStateMachine.stateMachines) {
                    if (machine.stateMachine.name == machine2.stateMachine.name) {
                        nMachine = machine2.stateMachine;
                        break;
                    }
                }

                BuildStateMapping(mapping, oMachine, nMachine);
            }
        }
    }
}