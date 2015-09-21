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


## API Coverage

Here is a probably-mostly-up-to-date list of implemented API calls:

- [ ] - Single Dimensional Data
    - [ ] - Employees
        - [ ] - Add an employee
        - [x] - Get an employee
        - [ ] - Update an employee
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
    - [ ] - Get a table
    - [ ] - Update a row
    - [ ] - Add a row
    - [ ] - Get tables for changed employees
- [] - Time Off
    - [x] - Get time off requests
    - [x] - Add a time off request
    - [ ] - Change a request status
    - [ ] - Add a time off history entry
    - [ ] - List assigned Time Off Policies
    - [ ] - Assign a new Time Off Policy
    - [ ] - Add a time off history override
    - [ ] - Estimate future time off balances
    - [ ] - Get a list of who's out, including company holidays
- [ ] - Photos
    - [x] - Get an employee photo
    - [ ] - Upload an employee photo
    - [-] - Using a photo from BambooHR's servers (Not fully tested)
- [ ] - Metadata
    - [x] - Get a list of fields
    - [x] - Get a list of tabular fields
    - [x] - Get the details for "list" fields in an account
    - [ ] - Add or update values for "list" fields in an account
    - [x] - Get a list of time off types
    - [x] - Get a list of time off policies
    - [x] - Get a list of users
- [ ] - Last Change Information
- [ ] - Login


## Caveats/Notes

This library doesn't do any caching for you, make sure you don't spam the API and go over your limit! There's probably a limit, right?



## Credit

Thanks to Stack Overflow for letting me (John Bubriski) open source this code.


## License

MIT, but check the LICENSE file in case I'm trolling you.