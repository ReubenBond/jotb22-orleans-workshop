# Lab 1: Hello, Orleans!

This lab is our first introduction to Orleans and it consists of creating a new .NET Console Application, installing Orleans, configuring Orleans, and running the application. The application merely starts up and waits for you to exit the application via Ctrl+C. Nevertheless, it acts as a basis for all subsequent labs.

## Goal

The goal of this lab is to help you get set up with a working Orleans application which we can build upon in subsequent labs.

## Steps

### Setup your environment

* Ensure you have the .NET SDK installed: https://dotnet.microsoft.com/en-us/download
* Ensure you have `git` installed
* Install an IDE of your choice:
  * Visual Studio
  * Visual Studio Code
  * Visual Studio for Mac
  * JetBrains Rider

* Fork & clone the [workshop repository](https://github.com/ReubenBond/jotb22-orleans-workshop) to a folder on your machine

  * Optionally click the *Fork* button so that you can push your progress to GitHub
  * Clone the repository

For the main fork, run `git clone https://github.com/ReubenBond/jotb22-orleans-workshop`

### Create a new .NET Console Application

Navigate to the `lab1` folder in the fork and create an new .NET Console application using the `dotnet` CLI

``` sh
dotnet new console
```

### Install the required packages

Add the following packages and the to your application

``` sh
dotnet add package Microsoft.Orleans.Server
dotnet add package Microsoft.Orleans.CodeGenerator.MSBuild
dotnet add package Microsoft.Extensions.Hosting
```

Here is what each package does
* `Microsoft.Orleans.Server` is a *metapackage* which includes the Orleans runtime and core libraries
* `Microsoft.Orleans.CodeGenerator.MSBuild` is a build-time dependency which adds the *C# source generator* which Orleans uses to generate helper code for your application, including
  * Grain reference implementations (known more generally as RPC proxies)
  * Serializers for the data classes you define
  * Metadata about your application, including which classes were generated and which `Grain` classes exist in your application
* `Microsoft.Orleans.Server` is a *metapackage* which includes the Orleans runtime and core libraries
* `Microsoft.Extensions.Hosting` is a generic hosting library used by many .NET libraries and applications. It defines extensible abstractions for creating a host which supports dependency injection, logging, configuration, and a host application lifetime (support for exiting the application when it receives Ctrl+C/`SIGINT`)

### Write the code

Open the project in your chosen IDE or text editor and edit *Program.cs* so that it has the following contents:

``` csharp
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

// Define a host builder with defaults for logging, configuration sources, and dependency injection.
var hostBuilder = Host.CreateDefaultBuilder(args);

// Add an Orleans silo to the host builder.
hostBuilder.UseOrleans(siloBuilder =>
{
    // Configure dev/test clustering for Orleans.
    siloBuilder.UseLocalhostClustering();
});

// Configure the host to listen to console events (Ctrl+C/SIGINT), build the host, run it, and wait for a shutdown event.
await hostBuilder.RunConsoleAsync();

```

### Run your application

After saving your changes to *Program.cs*, run your application from the terminal window

``` sh
dotnet run
```

You should see some logs flying by. Towards the bottom, notice the following two lines:

* `Orleans Silo started.`
* `Application started. Press Ctrl+C to shut down.`

This indicates that the application is running. You can press Ctrl+C to terminate the application. Once you do, it will take a few seconds to clean up and shutdown, printing out some diagnostic information before finally declaring:

* `Orleans Silo stopped.`

Congratulations, you have built your first Orleans application! It is not useful yet, so lets move on and start exploring the possibilities.
