# Crashout Crew - Localizer Fallbacks

This mod should be used as a dependency when you need to display text which 
falls outside the normal scope of localization. 
This can be, for example, displaying error messages or adding options.

Since it is extremely difficult to support custom localizations for Japanese and Chinese, this is 
also useful even when you do provide your own localizations but cannot target these languages.

By default, when the game encounters a localization id that it doesn't recognise, it simply returns a null string.
This means that the text you wanted to display just won't show up. 
There are two solutions to this problem. Firstly, you could add the localization id to the game's `LocalizedText.mainTable`.
This is fine for static strings but what about when you need to display a variable string like an error message? 
That's where this tool comes in. Rather than doing any complicated manual patching, simply add this mod as a dependency, 
then if a localization id can't be found, the function to get the text returns the id itself. 

So if you wanted to queue a message you could run
```csharp
PlayerMessageManager.QueueMessage("This is a test", true, true);
```
and the text `This is a test` would display correctly.

The only drawback to this method is it assumes one language for your mod.
If you know your mod (or parts of your mod) only targets a specific demographic of language speakers, 
this is the fastest way to avoid having to deal with localization.