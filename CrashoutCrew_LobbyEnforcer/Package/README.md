# Crashout Crew - Lobby Enforcer

This mod should be used as a dependency to force mod compatibility.
Because Crashout Crew is a networked game, often the mods we make end up touching both server and client side code.
When this happens, if only one side has the mod installed, we might end up with a conflict causing crashes or desync.

I ran into this while working on a different mod. My solution, a mod designed to perform a handshake on connection to 
verify the installed mods on both the client and server are compatible. If either side has this mod and a mod that is 
being enforced while the other side does not, the connection is rejected.

### Warning
Before I explain how to use Lobby Enforcer, a quick warning. This handshake is performed once the player is added to the lobby.
This means that LobbyManager's initialization functions have already been run by the time we verify the connection.
Make sure your mod is capable of coping with mods being different up to this point.

### Usage
To enforce a mod, simply add the lobby enforcer dll as a reference to your project, then run
```csharp
LobbyEnforcer.Enforcer.Enforce("my_plugin_guid", "X.Y.Z", EnforcementDirection.Both, "My Data String");
```
You must provide a plugin guid, version string, and enforcement direction. The enforcement direction can mostly be ignored 
as it will almost always need to be set to `EnforcementDirection.Both`. 

You can also optionally provide data in the form of a string. This can be used to limit connections to those with some shared state
which effectively allows a password system to be placed on the lobby.

If you need to update the data string after the enforcement has been established, you can use
```csharp
LobbyEnforcer.Enforcer.UpdateEnforceData("my_plugin_guid", "My Updated Data String");
```

Upon a failed connection, the client will be informed of the missing mods with an error message.