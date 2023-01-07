using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using System.IO;
using System;
using VRC.SDKBase;
using VRC.SDK3.Dynamics.Contact.Components;

namespace PickupAndWeaponSystem {
    public class ContactItemGrabSystem: EditorWindow
    {
        string itemName;

        private string animationSavePath = "Assets/sophia's pickups and weapon system/Generated/";

        AnimatorController fxAnimator;
        VRC_AvatarDescriptor avatar;
        GameObject worldConstraint;
        GameObject containerObject;
        GameObject itemPrefab;
        GameObject trackingPrefab;
        GameObject cullPrefab;
        GameObject targetPrefab;
        AnimatorController copyFromController;

        private GameObject trackingObject;
        private GameObject itemObject;

        private string animationsFolderPath;
        private string animationRetargetPath;

        Dictionary<string, bool> selectedLayers = new Dictionary<string, bool>();

        private bool worldConstraintExpanded;
        private bool defaultObjectsFoldoutExpanded;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Tayou/Contact Item Grab System")]
        static void Init()
        {
            ContactItemGrabSystem window = (ContactItemGrabSystem)GetWindow(typeof(ContactItemGrabSystem), false, "Contact Item Grab System");
            window.Show();
        }

        void OnDisable() { }

        void OnEnable() { }

        void OnGUI() {
            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            centeredStyle.alignment = TextAnchor.UpperCenter;
            centeredStyle.fontSize = 18;

            GUILayout.Label("Contact Item Grab System", centeredStyle);

            itemName = EditorGUILayout.TextField("Item Name", itemName);
            animationsFolderPath = animationSavePath + itemName + "/";
            Directory.CreateDirectory(animationSavePath);

            EditorGUILayout.BeginScrollView(scrollPosition);

            avatar = (VRC_AvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRC_AvatarDescriptor), true, new GUILayoutOption[] { });

#region World Constraint
            int worldConstraintFound;
            if (worldConstraint == null) { // No world constraint
                worldConstraintFound = 1;
                worldConstraintExpanded = true;
            } else if (!avatar.transform.Find(worldConstraint.name)) { //its not in Avatar
                worldConstraintFound = 2;
                worldConstraintExpanded = true;
            } else {
                worldConstraintFound = 0;
            }
            worldConstraintExpanded = EditorGUILayout.Foldout(worldConstraintExpanded, "World Constraint");
            if (worldConstraintExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(new GUIContent(worldConstraintFound == 2 ? "Prefab not found in Avatar, Press button below to Instantiate on Avatar" : "Assign this to Either a Prefab for the world Constraint or a existing World Constraint under your Avatar hierarchy"));
                worldConstraint = (GameObject)EditorGUILayout.ObjectField("World Constraint", worldConstraint, typeof(GameObject), true, new GUILayoutOption[] { });
                if (GUILayout.Button("Set up World Constraint!")) {
                    FindAndPlacePrefabs();
                }
                EditorGUI.indentLevel--;
            }
            GUI.enabled = worldConstraintFound == 0;
#endregion

            itemPrefab = (GameObject)EditorGUILayout.ObjectField("Item Prefab", itemPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
            trackingPrefab = (GameObject)EditorGUILayout.ObjectField("Tracking Prefab", trackingPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
            cullPrefab = (GameObject)EditorGUILayout.ObjectField("Cull Object", cullPrefab, typeof(GameObject), true, new GUILayoutOption[] { });
            targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Object", targetPrefab, typeof(GameObject), true, new GUILayoutOption[] { });

            if (GUILayout.Button("Set up GameObjects!")) {
                FindAndPlacePrefabs();
            }

            fxAnimator = (AnimatorController)EditorGUILayout.ObjectField("FX Controller", fxAnimator, typeof(AnimatorController), true, new GUILayoutOption[] { });
            copyFromController = (AnimatorController)EditorGUILayout.ObjectField("Animator to copy from", copyFromController, typeof(AnimatorController), true, new GUILayoutOption[] { });
            if (GUILayout.Button("Set up Animator!")) {
                AdjustControllerLayers();
            }

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

            EditorGUILayout.EndScrollView();
        }

        void AdjustControllerLayers() {
            if (!copyFromController || !fxAnimator) return;

            selectedLayers = new Dictionary<string, bool>();
            bool firstLayer = false;
            foreach (var layer in copyFromController.layers) {
                selectedLayers.Add(layer.name, firstLayer);
                firstLayer = true;
            }
            //, new int[]{1, 2, 3}, animationSavePath, ("SophiaItemSys", "SophiaItemSys/" + itemName)
            Copy(copyFromController, fxAnimator, PreProcessParameter, PostProcessTransitions);
            PrintLog("Layers Pasted");

        }

        /// <summary>
        /// Copies a given Animation Clip from its original Location to a Location made out of the original Path + itemName
        /// TODO: generate output path from given input path correctly in UPM package
        /// </summary>
        /// <param name="animationClip"></param>
        /// <returns></returns>
        private AnimationClip CopyAnimationClip(AnimationClip animationClip) {
            // TODO: this line needs updating for the UPM package, needs to create a new path based on the original folder, not based on hardcoded path
            string newAnimationClipPath = AssetDatabase.GetAssetPath(animationClip).Replace("Assets/sophia's pickups and weapon system/Setup Tool/AnimationAssets/", animationsFolderPath);
            Directory.CreateDirectory(Path.GetDirectoryName(newAnimationClipPath));
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(animationClip), newAnimationClipPath)) {
                Debug.LogWarning("Copy Failed");
            }
            AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(newAnimationClipPath, typeof(AnimationClip));
            if (clip == null) {
                PrintLog("Loading Copied Asset from " + newAnimationClipPath + " was not successful, Abort!");
            } else {
                PrintLog("Copied Animation from " + AssetDatabase.GetAssetPath(animationClip) + " to " + newAnimationClipPath);
            }

            return clip;
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
            for (int i = 0; i < editorCuriveBinding.Length; i++) {
                EditorGUILayout.BeginHorizontal();
                EditorCurveBinding item = editorCuriveBinding[i];
                if (item.path.Contains("localEulerAnglesRaw")) {
                    continue;
                }

                string newPath = item.path;
                if (item.path.Contains("sophia's hand tracker/Pickup holder/Item 1")) {
                    newPath = item.path.Replace("sophia's hand tracker/Pickup holder/Item 1", AnimationUtility.CalculateTransformPath(itemObject.transform, avatar.transform));
                } else if (item.path.Contains("sophia's hand tracker/Cull")) {
                    newPath = item.path.Replace("sophia's hand tracker/Cull", AnimationUtility.CalculateTransformPath(cullPrefab.transform, avatar.transform));
                } else if (item.path.Contains("sophia's hand tracker/World/item 1 tracking")) {
                    newPath = item.path.Replace("sophia's hand tracker/World/item 1 tracking", AnimationUtility.CalculateTransformPath(trackingObject.transform, avatar.transform));

                }

                animationClip.SetCurve(newPath  , item.type, item.propertyName, AnimationUtility.GetEditorCurve(animationClip, item));
                animationClip.SetCurve(item.path, item.type, item.propertyName, null);
            }
            return animationClip;
        }

        /// <summary>
        /// Ratargets Given Constraint to new Transform
        /// </summary>
        /// <param name="constraint">Constraint to Modify</param>
        /// <param name="targetPrefab">Target Prefab to constrain to</param>
        /// <param name="index">Index Position in Constraint Sources</param>
        void RetargetConstraint(ParentConstraint constraint, GameObject targetPrefab, int index) {
            ConstraintSource owo = constraint.GetSource(index);
            owo.sourceTransform = targetPrefab.transform;
            constraint.SetSource(0, owo);

            PrintLog("Constraint Retargetted");
        }

        /// <summary>
        /// TODO: place prefabs into one common parent for easy organization
        /// maybe use single prefab for instantiation too, makes it harder to mess up references between the various prefabs
        /// -> WorldPrefab 
        ///     -> Cull
        ///     -> "itemName" 
        ///         -> Tracking
        ///         -> Target
        ///         -> Item
        /// </summary>
        void FindAndPlacePrefabs() {
            // Item Names
            string cullObjectName = "Cull";
            string objectContainerName = itemName;
            string targetObjectName = itemName + " Target";
            string trackingObjectName = itemName + " Tracking";
            string itemObjectName = itemName;


            // Place World Constraint (if not placed already)
            if (!worldConstraint.transform.IsChildOf(avatar.transform)) {
                worldConstraint = Instantiate(worldConstraint, avatar.transform);
            }

            // Place Cull Object, which prevents the avatar from being culled, in order to prevent the item from desyncing
            if (!cullPrefab.transform.IsChildOf(worldConstraint.transform)) {
                cullPrefab = Instantiate(cullPrefab, worldConstraint.transform);
                cullPrefab.name = cullObjectName;
            }

            // Place Empty Parent Object
            if ((object)containerObject == null) {
                containerObject = new GameObject(objectContainerName);
                containerObject.transform.parent = worldConstraint.transform;
                containerObject.transform.localPosition = Vector3.zero;
            } else if (containerObject.name != objectContainerName) { 
                containerObject.name = objectContainerName;
            }

            #region item specific
            {
                // Place Target Prefab
                if (!targetPrefab.transform.IsChildOf(worldConstraint.transform)) {
                    targetPrefab = Instantiate(targetPrefab, worldConstraint.transform);
                    targetPrefab.name = targetObjectName;
                } else if (targetPrefab.name != targetObjectName) {
                    containerObject.name = targetObjectName;
                }

                // Place Tracking Prefab
                if (!trackingPrefab.transform.IsChildOf(worldConstraint.transform)) {
                    trackingPrefab = Instantiate(trackingPrefab, worldConstraint.transform);
                    trackingPrefab.name = trackingObjectName;
                } else if (trackingPrefab.name != trackingObjectName) {
                    trackingPrefab.name = trackingObjectName;
                }

                // Place Item Prefab
                if (!itemPrefab.transform.IsChildOf(worldConstraint.transform)) {
                    itemPrefab = Instantiate(itemPrefab, worldConstraint.transform);
                    itemPrefab.name = itemObjectName;
                } else if (itemPrefab.name != itemObjectName) {
                    itemPrefab.name = itemObjectName;
                }
            }
            #endregion

            PrintLog("Prefabs Placed");

            RetargetConstraint(trackingObject.transform.Find("object").GetComponent<ParentConstraint>(), targetPrefab, 0);
        }

        /// <summary>
        /// TODO: only change the ones, that belong to the item (check if path includes itemName maybe?)
        /// </summary>
        /// <param name="first">Search root (currently not used as Resources.FindObjectsOfTypeAll searches everywhere)</param>
        void UpdateContactReceivers(GameObject first) {
            VRCContactReceiver[] contactReceivers = Resources.FindObjectsOfTypeAll<VRCContactReceiver>();
            foreach (var item in contactReceivers) {
                item.parameter = item.parameter.Replace("SophiaItemSys", "SophiaItemSys/" + itemName);
            }

        }

        private void PrintLog(string text) {
            Debug.Log("<color=#BB22FF>Contact Item Grab Setup Tool</color>: " + text);
        }

        private AnimatorControllerParameter PreProcessParameter(AnimatorControllerParameter parameter) {
            if (parameter.name.Contains("SophiaItemSys")) {
                return new AnimatorControllerParameter() { name = parameter.name.Replace("SophiaItemSys", "SophiaItemSys/" + itemName), type= parameter.type, defaultBool = parameter.defaultBool, defaultFloat = parameter.defaultFloat, defaultInt = parameter.defaultInt };
            }
            return null;
        }
        
        private void PostProcessTransitions(AnimatorTransitionBase[] transitions, Func<AnimatorState, AnimatorTransitionBase> newTransition, Action<AnimatorTransitionBase> removeTransition) {
            foreach (AnimatorTransitionBase tranistion in transitions) {
                foreach (AnimatorCondition condition in tranistion.conditions) {
                    if (condition.parameter.Contains("SophiaItemSys")) {
                        tranistion.RemoveCondition(condition);
                        tranistion.AddCondition(condition.mode, condition.threshold, condition.parameter.Replace("SophiaItemSys", "SophiaItemSys/" + itemName));
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
                if (!selectedLayers[layer.name])
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
                if (!selectedLayers[layerName]) {
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
                if (!selectedLayers[layer.name])
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
                if (!selectedLayers[layer.name])
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
            InspectTrainsitions(paramList, stateMachine.anyStateTransitions);

            //Entry Transitions
            InspectTrainsitions(paramList, stateMachine.entryTransitions);

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
                InspectTrainsitions(paramList, state.state.transitions);
            }

            //Child StateMachines
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines) {
                CollectParameters(paramList, layerList, childStateMachine.stateMachine);

                //Trainsitions
                InspectTrainsitions(paramList, stateMachine.GetStateMachineTransitions(childStateMachine.stateMachine));
            }
            if (paramList.Contains("ContactTracker")) {
                Debug.Log("Its here!!!" + paramList.IndexOf("ContactTracker"));
            }
        }

        private void InspectTrainsitions(List<string> paramList, AnimatorTransitionBase[] transitions) {
            foreach (AnimatorTransitionBase transition in transitions) {
                foreach (AnimatorCondition condition in transition.conditions) {
                    if (!paramList.Contains(condition.parameter))
                        paramList.Add(condition.parameter);
                }
            }
        }

        private void InspectMotion(List<string> paramList, Motion motion) {
            if (motion is BlendTree)
                InspectBlendTree(paramList, motion as BlendTree);
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

            for (int i = 0; i < tree.children.Length; i++)
                InspectMotion(paramList, tree.children[i].motion);
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
                if (renamedParameters.ContainsKey(oState.cycleOffsetParameter))
                    nState.cycleOffsetParameter = renamedParameters[oState.cycleOffsetParameter];
                else
                    nState.cycleOffsetParameter = oState.cycleOffsetParameter;
                nState.cycleOffsetParameterActive = oState.cycleOffsetParameterActive;

                nState.iKOnFeet = oState.iKOnFeet;

                //Mirror
                nState.mirror = oState.mirror;
                if (renamedParameters.ContainsKey(oState.mirrorParameter))
                    nState.mirrorParameter = renamedParameters[oState.mirrorParameter];
                else
                    nState.mirrorParameter = oState.mirrorParameter;
                nState.mirrorParameterActive = nState.mirrorParameterActive;

                //Speed
                nState.speed = oState.speed;
                if (renamedParameters.ContainsKey(oState.speedParameter))
                    nState.speedParameter = renamedParameters[oState.speedParameter];
                else
                    nState.speedParameter = oState.speedParameter;
                nState.speedParameterActive = oState.speedParameterActive;

                nState.tag = oState.tag;

                //Time
                if (renamedParameters.ContainsKey(oState.timeParameter))
                    nState.timeParameter = renamedParameters[oState.timeParameter];
                else
                    nState.timeParameter = oState.timeParameter;
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
                AnimatorTransition nTransition;
                if (oTransition.destinationState != null)
                    nTransition = dstStateMachine.AddEntryTransition(stateMapping[oTransition.destinationState]);
                else
                    nTransition = dstStateMachine.AddEntryTransition(machineMapping[oTransition.destinationStateMachine]);

                CopyTransition(oTransition, nTransition, renamedParameters);
            }
            //PostProcess
            transitionPostProcessor?.Invoke(srcStateMachine.entryTransitions, target => dstStateMachine.AddEntryTransition(target), x => dstStateMachine.RemoveEntryTransition((AnimatorTransition)x));

            //Copy AnyState Transitions
            foreach (AnimatorStateTransition oTransition in srcStateMachine.anyStateTransitions) {
                AnimatorStateTransition nTransition;
                if (oTransition.destinationState != null)
                    nTransition = dstStateMachine.AddAnyStateTransition(stateMapping[oTransition.destinationState]);
                else
                    nTransition = dstStateMachine.AddAnyStateTransition(machineMapping[oTransition.destinationStateMachine]);

                CopyStateTransition(oTransition, nTransition, renamedParameters);
            }
            //PostProcess
            transitionPostProcessor?.Invoke(dstStateMachine.anyStateTransitions, target => dstStateMachine.AddAnyStateTransition(target), x => dstStateMachine.RemoveAnyStateTransition((AnimatorStateTransition)x));

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
                if (renamedParameters.ContainsKey(condition.parameter))
                    dstTransition.AddCondition(condition.mode, condition.threshold, renamedParameters[condition.parameter]);
                else
                    dstTransition.AddCondition(condition.mode, condition.threshold, condition.parameter);
            }
        }

        Motion DeepCopyMotion(AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine, Motion motion, Dictionary<string, string> renamedParameters) {
            if (motion == null)
                return null;
            else if (motion is AnimationClip)
                return UpdateAnimationClipPath(CopyAnimationClip(motion as AnimationClip));
            else if (motion is BlendTree) {
                //Is path 
                if (AssetDatabase.GetAssetPath(srcStateMachine) == AssetDatabase.GetAssetPath(motion))
                    return DeepCopyBlendTree(srcStateMachine, dstStateMachine, motion as BlendTree, renamedParameters);
                else
                    return motion;
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