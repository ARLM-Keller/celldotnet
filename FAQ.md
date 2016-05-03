# Frequently Asked Questions #

Q: Is this a complete .NET environment for the SPEs?

A: No. Mono provides much of this for the PPE, but on the SPEs there are restrictions on what can be done. See [Introduction](Introduction.md).


Q: Will this ever become a complete .NET environment for the SPEs?

A: Maybe, but probably not. The SPEs are not general purpose processors, and pretending that they are would be hard.

Q: How does this project relate to Mono?

A: CellDotNet runs on top of Mono. Mono runs on the PPE, and CellDotNet generates and supports code running on the SPEs.