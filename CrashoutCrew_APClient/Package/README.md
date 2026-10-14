# Crashout Crew - Archipelago Client

This mod can and SHOULD be installed through the [ThunderStore Mod Manager](https://thunderstore.io/c/crashout-crew/)

Manual installation is possible but at your own peril. 

### Other Dependencies 
- [Sky's The Limit]()
- [Lobby Enforcer]()
- [Localizer Fallbacks]()

## Implementation Details

[The apworld can be found here](https://github.com/xMcacutt-Archipelago/Archipelago-VeryVeryValet/releases/latest)

### Connecting
***WARNING*** - You must launch the game through steam for this mod to work. Launching from the executable will make it impossible to enter to a lobby.

When you first boot into the mod, the connect and join game buttons will both be grayed out.
Open the options menu and connect with the fields at the top. Once you're connected, close the menu and either press continue to host a game
or open the steam overlay with shift+tab to connect to a friend.

### Multiplayer Support
When connecting to a friend, you must both connect to the slot in the options menu 
from the main menu, then one player presses continue and the other joins through the steam overlay.

### Contracts

Each level in Crashout Crew is a contract. Every contract has some number of orders (items that can make up deliveries).
For the randomizer, all contracts are... well randomized. They are constructed based on the number of contracts you 
add to your yaml as well as the seed of the multiworld. The goal is to complete the final contract.

### Bells

To unlock more contracts, you'll need bells. Each contract requires five bells to unlock up to a maximum of 60 for the maximum
of 15 contracts. Theoretically, we can go higher but the game would just take too long for Archipelago.

### Deliveries

Sending deliveries and completing shifts are your checks. Each time you send a truck in a contract, that's a check. 
When you send all trucks and go back to the shop, that's a completed shift, and you send a check.

### The Shop
At the start of a new multiworld, you won't have anything unlocked in the shop. Each possible item which can appear
in the shop is an item you have to be sent before it can be stocked. Get creative with the items you have available.

### Upgrades
Throughout the multiworld, there are also three upgrades scattered in each of four categories:

Base Speed Upgrade - Gives you more speed (also increases turning radius)

Base Stress Upgrade - Allows you to take more stress without crashing out.

Base Boost Upgrade - You start with one nitro boost and upgrade to a maximum base of four.

Base Strength Upgrade - You start only being able to carry three boxes at a time and upgrade to six!

### Cosmetics
There's only one filler in this implementation currently: `Random Cosmetic`.
When you receive a random cosmetic, your appearance might change.