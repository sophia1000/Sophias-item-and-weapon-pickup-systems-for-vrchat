# Sophia's Item And Weapon Pickup System
Based off red's / VRLabs [contact trackers system](https://github.com/VRLabs/Contact-Tracker). This is a system for vrchat that allows others to grab and manipulate an object or weapon on your avatar as though it was a part of the world.

## Instructions
### Prerequisites - Please Have Imported First
https://github.com/sophia1000/Sophias-item-and-weapon-pickup-systems-for-vrchat
- Install latest version via UPM by adding as git package using this url: `https://github.com/sophia1000/Sophias-item-and-weapon-pickup-systems-for-vrchat`:   
    ![image](https://user-images.githubusercontent.com/31988415/214831214-3d94ed23-c084-47fc-985f-5c9a8e317d35.png)   
    ![image](https://user-images.githubusercontent.com/31988415/214831368-17da6210-1b59-4df4-a984-69007b6e8a4f.png)   
    or get the latest version from [Releases](https://github.com/sophia1000/Sophias-item-and-weapon-pickup-systems-for-vrchat/releases) and put it into the `Packages` Folder in your Project **(not `Assets`!)**
- If you don't own Final IK, download [VRLabs FinalIK stub](https://github.com/VRLabs/Final-IK-Stub) It will let you upload the system to your avatar even if you don't have Final IK.   
    But note that you will only be able to test in unity if you have the [full Final IK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290).
- Ensure you have the VRChat Avatar SDK installed, the following packages are needed: `com.vrchat.base`, `com.vrchat.avatars`   
    Get them from the VRChat Creator Companion or manually from [VRChat.com](https://vrchat.com/home/download)

### Automatic
1. Open the Editor Tool from the menubar -> Tools -> Sophia -> Item Pickup System Setup Tool
2. Enter the name of your Pickup object into the `Name` slot
3. Assign the avatar descriptor in the slot at the top
4. If you already have a world constraint on your avatar, put it in the slot. Otherwise hit the button to place one.
5. Now hit the button to place the Cull object. This is used to ensure your objects position is synced correctly across clients
6. Now hit the button to place Pickup System, this will place a few objects inside the World Constraint
7. Now hit the button to add the animator layers to your FX controller. Should this controller not be assigned automatically you may add it into the slot here.
8. Now make a animation to toggle the entire system on and off.
9. And add your item into `<Your Avatar>/World/<Item Name>/Item/Container/`. You may want to use the sample item inside there to see, where and how to place your item.
    After placing your item make sure to remove the existing item or mark it as `EditorOnly`

### Manual
- (Optional) Install [VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager) or [AirGamers AnimatorLayerCopy](https://github.com/TheLastRar/AnimatorLayerCopy) to merge fx layers.

 1. Drag the prefab into the hierarchy.
 2. Right-click on the prefab to unpack it and then drag it onto the base of your avatar
 3. Enable the "Container" under Sword Holder/constraint target and place your desired prop into the "PUT ITEM IN HERE" object or just use the sword that comes with it.
 4. If you are using your own item, position it to where the sword's hilt is so it will be grabbed the same way.
 5. Hide the container object.
 6. Merge the provided FX controller with yours using the [VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager)
 7. There are 2 fx layers provided one with wite defaults on and one with write defaults off. Use the one that works for your setup
 8. Create a toggle in your menu. there's an example one provided

## How It Works
By using PhysBones to get the position of the hand, it can connect a series of proximity contacts around your hand and by using the position of your fingers and hands it can then tell where your hand is and how its positioned.

## Credit
[red's / vrlabs tracker system](https://github.com/VRLabs/Contact-Tracker)   
[Tayou / Making the setup script](https://github.com/TayouVR)   
[AirGamer / Providing LayerCopy script, used internally](https://github.com/TheLastRar/AnimatorLayerCopy)   

Default sword modelled by Meltingarmymen

## License
MIT

## Contact
little.sophia#1000
If you make something cool with this, please message me. I wanna see what people make of it. if you have any ideas please let me know.

## To-do list
I plan to add more weapons to it like a functional gun and bow.

Potentially figure out how to get aim IK to work better so there won't be a need for Final IK.

Add better smoothing so finger movement has less of an effect on it.

## Contributing
If you wish to contribute to the Script or prefab please test the script after applying your modifications.

For that, make sure you have the following packages installed:
- `com.vrchat.base` (VCC)
- `com.vrchat.avatars` (VCC)
- `com.unity.upm.develop` (UPM)

When doing any contributions make sure to to go the package manager UI, select this package and click the "Test" button in the bottom left corner.
If you have done any changes, that change behaviour of the script you may need to update the test script as well.

The test script will perform a testing procedure to ensure the functionality of the script is not compromised.
Alternatively/In Addition you may want to also manually use the script to make sure the UI is fully functional.
