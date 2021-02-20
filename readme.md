# TinyTunnel (SSH Port Forwarder)

Made this quick n dirty tool using C# .NET so I can administer multiple firewalled servers from home.
I had been using Putty for those tasks under Windows but found out soon that it's GUI is very crude
and quite honestly buggy.
So I made this to make my port forwarding tasks easier (and showcase some of my works on github).

## Features
- Command line tool that simply runs
- Supports INI file format for editing your connections with any editor, along with your comments
- Based on the well trusted C# .NET Renci SSH.NET library
- Supports the native OpenSSH private key formats
- Supports either private key file based authentication or simple user / pass authentication
- Supports setting Local listen IP and Port (in case you want to set to a different local network interface)
- Supports setting Remote network IP interface and Port (Basically for using internal network of server)
- Supports enabling or disabling each connection with a flag.

### License
GPL

##### Author
Simon Kapsalis

