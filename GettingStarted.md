# Introduction #

Getting up and running with CellDotNet is not very hard, but you need to be comfortable with Linux and .NET.


# Details #

To get up and running you need a couple of things:

  * A Cell procesor in some form, either in a PlayStation 3 or in an IBM BladeServer.
  * Linux
  * Mono
  * libspe
  * C5
  * CellDotNet

The procedure goes something like this:

  1. Install a PowerPC Linux. We have used Yellow Dog and Fedora Core 7, but other distributions will probably work. There is no need to configure the kernel for huge pages.
  1. Install Mono, version >= 1.2.5.
> > http://www.mono-project.com/
  1. Get a copy of the SPE management library, libspe, version >= 2. The Cell SDK available from IBM provides this, along with development tools which can become handy. SDK v3 is the current version, but SDK version 2.x will also do, if you happen to have that installed.
> > http://www-128.ibm.com/developerworks/power/cell/
  1. Get the C5 library. Mono ships with this, so depending on how you want to compile CellDotNet, you might not need this.
> > http://www.itu.dk/research/c5/
  1. Get the CellDotNet sources from subversion or the download section.
> > Subversion location: http://celldotnet.googlecode.com/svn/trunk/ or http://celldotnet.googlecode.com/svn/tags/v0.11
> > Download: http://code.google.com/p/celldotnet/downloads/list






Now you're ready to compile CellDotNet. We use Visual Studio 2008 for the development, but Mono can obviously also be used.

Now you're ready to try it out. The easiest way to experiment is by modifying Class1.cs. Look at the unit tests to get an idea of what's currently possible to do.