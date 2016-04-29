# BambooHR .NET Client

[![Build Status](https://img.shields.io/appveyor/ci/jbubriski/bamboo-hr-dotnet-client.svg)](https://ci.appveyor.com/project/jbubriski/bamboo-hr-dotnet-client) [![License LGPLv3](https://img.shields.io/badge/license-mit-green.svg)](https://raw.githubusercontent.com/jbubriski/bamboo-hr-dotnet-client/master/LICENSE.md)

A .NET client for the BambooHR REST API.


## NOTICE:

**Bamboo HR recently removed support for SSLv3 and TLSv1.0, so if you're running .Net < 4.6.1, you'll need to add this line to your application somewhere, probably inside Application_Start() in your Global.asax.  Keep in mind that this is a global change to your applcation.  If there are any other calls you make to HTTPS endpoints that only support SSLv3, they will stop working.  Most services probably don't even support SSLv3 anymore anyway, [read this CloudFlare post for more information on the subject](https://blog.cloudflare.com/sslv3-support-disabled-by-default-due-to-vulnerability/).  Thanks goes to the Nifty [Nick Craver](http://nickcraver.com/) for the fix.**

    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
        SecurityProtocolType.Tls11 |
        SecurityProtocolType.Tls12;

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
    <add key="BambooCompanyUrl" value="https://examplecompanyname.bamboohr.com" />

If you're using this in the context of a web application project or website project, just add those lines to the `<appSettings>` section of your web.config.

At some point, I may provide a programmatic way to configure this information for use in other scenarios???  What **are** those other scenarios!?!?!?


## API Coverage

Here is a probably-mostly-up-to-date list of implemented API calls:

- [ ] - Single Dimensional Data
    - [-] - Employees
        - [x] - Add an employee
        - [x] - Get an employee
        - [x] - Update an employee
        - [-] - Get a directory of employees (sort of, through custom report)
    - [ ] - Reports
        - [ ] - Request a company report
        - [ ] - Request a custom report
    - [ ] - Employee Files
        - [ ] - List employee files and categories
        - [ ] - Add an employee file category
        - [ ] - Update an employee file
        - [ ] - Download an employee file
        - [ ] - Upload an employee file
    - [ ] - Company Files
        - [ ] - List company files and categories
        - [ ] - Add a company file category
        - [ ] - Update a company file
        - [ ] - Download a company file
        - [ ] - Upload a company file
- [ ] - Tabular Data
    - [x] - Get a table
    - [ ] - Update a row
    - [ ] - Add a row
    - [ ] - Get tables for changed employees
- [ ] - Time Off
    - [x] - Get time off requests
    - [x] - Add a time off request
    - [ ] - Change a request status
    - [ ] - Add a time off history entry
    - [x] - List assigned Time Off Policies
    - [ ] - Assign a new Time Off Policy
    - [ ] - Add a time off history override
    - [x] - Estimate future time off balances
    - [x] - Get a list of who's out, including company holidays
- [x] - Photos
    - [x] - Get an employee photo
    - [x] - Upload an employee photo
    - [x] - Using a photo from BambooHR's servers
- [ ] - Metadata
    - [x] - Get a list of fields
    - [x] - Get a list of tabular fields
    - [x] - Get the details for "list" fields in an account
    - [ ] - Add or update values for "list" fields in an account
    - [x] - Get a list of time off types
    - [x] - Get a list of time off policies
    - [x] - Get a list of users
- [x] - Last Change Information
- [ ] - Login


## Caveats/Notes

This library doesn't do any caching for you, make sure you don't spam the API and go over your limit! There's probably a limit, right?



## Credit

Thanks to Stack Overflow for letting me (John Bubriski) open source this code.


## License

MIT, but check the LICENSE file in case I'm trolling you.
