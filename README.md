# Sophia's-Item-And-Weapon-System
based of of red's / vrlabs [contact trackers system](https://github.com/VRLabs/Contact-Tracker). This is a system for vrchat that allows others to grab and manipulate a object or weapon on your avatar as though it was a part of the world.

# Prerequisites Please Have Imporeted First
[VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager) to merge fx layers

If you dont already have Final Ik download [VRLabs FinalIK stub](https://github.com/VRLabs/Final-IK-Stub) it will let you upload the system to your avatar even if you dont have final ik but you will only be able to test in unity if you have the full final ik



# instructions
Drag the prefab into the heirarchy

Right click on the prefab to unpack it and then drag it onto the base of your avatar

Enable the "Container" under Sword Holder/constraint target and place your desired prop into it or just use the sword that comes with it.

if you are useing your own item position it to were the swords hilt is so it will be grabed the same way.

Hide the container object 

Merge the provided FX controller with yours using the [VRLabs Avatar 3.0 manager](https://github.com/VRLabs/Avatars-3.0-Manager)

there are 2 fx layers provided one with wd on and one with wd off use the one that works for your setup

create a toggle in you menu theres an exaple one provided


# How It Works
By useing physbones to get the position of the hand it can the conect a series of proximity contacts around your hand and by useing the position of your fingers and hands it can then tell were your hand it and how its positiond.

# Credit
[red's / vrlabs tracker system](https://github.com/VRLabs/Contact-Tracker)

default sword modled by Meltingarmymen

# License
MIT

# Contact
little.sophia#1000 if you make anything cool with this please message me i wanna see what people make of it. if you have any ideas please let me know.

# To do list
I plan to add more wepons to it like a funcional gun and bow.

potentialy figure out how to get aim ik to work better so there wont be a need for final ik.

Add better smoothing so finger movment has less of an effect on it.
