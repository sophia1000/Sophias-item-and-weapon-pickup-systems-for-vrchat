# Sophia's-Item-And-Weapon-System
Based off red's / VRLabs [contact trackers system](https://github.com/VRLabs/Contact-Tracker). This is a system for vrchat that allows others to grab and manipulate an object or weapon on your avatar as though it was a part of the world.

# Prerequisites Please Have Imported First

Get the latest version from [Releases](https://github.com/sophia1000/Sophias-item-and-weapon-pickup-systems-for-vrchat/releases) v1 was bugged so if you downloaded it please update

[VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager) to merge fx layers.

If you don't own Final Ik, download [VRLabs FinalIK stub](https://github.com/VRLabs/Final-IK-Stub) It will let you upload the system to your avatar even if you don't have Final IK. But note that you will only be able to test in unity if you have the full Final IK.



# instructions

 1. Drag the prefab into the hierarchy.
 2. Right-click on the prefab to unpack it and then drag it onto the base of your avatar
 3. Enable the "Container" under Sword Holder/constraint target and place your desired prop into the "PUT ITEM IN HERE" object or just use the sword that comes with it.
 4. If you are using your own item, position it to where the sword's hilt is so it will be grabbed the same way.
 5. Hide the container object.
 6. Merge the provided FX controller with yours using the [VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager)
 7. There are 2 fx layers provided one with wite defaults on and one with write defaults off. Use the one that works for your setup
 8. Create a toggle in your menu. there's an example one provided

# How It Works
By using PhysBones to get the position of the hand, it can connect a series of proximity contacts around your hand and by using the position of your fingers and hands it can then tell where your hand is and how its positioned.

# Credit
[red's / vrlabs tracker system](https://github.com/VRLabs/Contact-Tracker)

Default sword modelled by Meltingarmymen

# License
MIT


# To-do list
I plan to add more weapons to it like a functional gun and bow.

Potentially figure out how to get aim IK to work better so there won't be a need for Final IK.

Add better smoothing so finger movement has less of an effect on it.
