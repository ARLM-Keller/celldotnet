# Introduction #

This project is making it possible to run parts of .net programs on the vector processing units (the "SPU"s) of the Cell processor, which is where the horsepower of the processor is.

Of course, this projects builds on Mono running on standard Linux.

# Details #

The idea is to be able to offload computations to SPUs. Therefore, we are making it possible for a developer to write a managed method and in a single line of code call it in a way that makes it execute on an SPU.

Since these vector units are not general-purpose, but well-suited for computationally intensive code rather than branch-intensive code, we will not support arbitrary .net code. Some restrictions apply, including these:

  * Exceptions are not supported.
  * System.String is not supported.
  * Virtual methods are not supported.
  * Array bounds are not checked.
  * Most unsafe code is not supported.
  * There is no garbage collection. Once allocated, an object will occupy memory until the next program/routine is started.
  * Casting and boxing/unboxing is not supported.

These constraints can seem severily limiting, but for the kind of code that will benefit from running on an SPU they will often not be a problem.

Another aspect of the hardware is that, in order to use it efficiently, the programmer must assume control of memory transfers between main memory and the SPU's local memory. Therefore, essential hardware services are made available to the programmer.

The implementation is a library which JIT-compiles IL-code to native SPU code. The library is written in C# and only depends on a .net runtime, the basic SPU management library and, of course, a Cell processor.