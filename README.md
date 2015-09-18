# BambooHR .NET Client

A .NET client for the BambooHR REST API.


## Getting Started

A **demo project** is included!

1. Open `Program.cs` and uncomment any demo calls you want to make.
2. Open the App.config and fill in the blanks with information from your API account.
3. Hit F5 to run the console app.
4. Bask in the gloriously incandescent data gifted unto to you by your affordable and friendly HR system.


## Config (Just to spell it out)

For the demo project, make sure you update the App.Config with your API stuffs.  It will look something like this:

    <add key="BambooApiUser" value="email@example.com" />
    <add key="BambooApiKey" value="sadjlksdfguoi45u498j0sdfjoiksdfj08sdf" />
    <add key="BambooApiUrl" value="https://api.bamboohr.com/api/gateway.php/examplecompanyname/v1" />

If you're using this in the context of a web application project or website project, just add those lines to the `<appSettings>` section of your web.config.

At some point, I may provide a programmatic way to configure this information for use in other scenarios???  What **are** those other scenarios!?!?!?


## Caveats/Notes

This library doesn't do any caching for you, make sure you don't spam the API and go over your limit! There's probably a limit, right?



## Credit

Thanks to Stack Overflow for letting me (John Bubriski) open source this code.


## License

MIT, but check the LICENSE file in case I'm trolling you.