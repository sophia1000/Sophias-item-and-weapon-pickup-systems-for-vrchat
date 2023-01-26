# Documentation

## Usage

### Setting up a Item for the first time:
1. Open the Editor Tool from the menubar -> "Tools" -> Sophia -> Item Pickup System Setup Tool
2. Enter the name of your Pickup object into the `Name` slot
3. Assign the avatar descriptor in the slot at the top
4. If you already have a world constraint on your avatar, put it in the slot. Otherwise hit the button to place one.
5. Now hit the button to place the Cull object. This is used to ensure your objects position is synced correctly across clients. (If you already have a cull object on your avatar, put it in the slot.)
6. Now hit the button to place Pickup System, this will place a few objects inside the World Constraint
7. Now hit the button to add the animator layers to your FX controller. Should this controller not be assigned automatically you may add it into the slot here.
8. Now make a animation to toggle the entire system on and off.
9. And add your item into `<Your Avatar>/World/<Item Name>/Item/Container/`. You may want to use the sample item inside there to see, where and how to place your item.
    After placing your item make sure to remove the existing item or mark it as `EditorOnly`

### Modifying a existing Item:
changing the names or any other properties of the item after the fact currently has to be done manually, though this functionality may be added at a later point.

## Troubleshooting
// TODO

## Contributing
If you wish to contribute to the Script or prefab please test the script after applying your modifications.

For that, make sure you have the following packages installed:
- `com.vrchat.base`
- `com.vrchat.avatars`
- `com.unity.upm.develop`

When doing any contributions make sure to to go the package manager UI, select this package and click the "Test" button in the bottom left corner.
If you have done any changes, that change behaviour of the script you may need to update the test script as well.

The test script will perform a testing procedure to ensure the functionality of the script is not compromised.
Alternatively/In Addition you may want to also manually use the script to make sure the UI is fully functional.