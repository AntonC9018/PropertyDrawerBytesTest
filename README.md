
The idea was to support serialization of proper tagged unions in Unity.
Currently, the only viable way to do tagged unions in Unity is to make a struct that has a field for every type of value to be available for use.
This is suboptimal, because it takes up more memory than necessary to do this.
A better way would be to store the value inline, that is, have a fixed size byte array to hold it, and then reinterpret it on access for a specific type.

If the bytes are to be stored as-is by Unity's serialization system, or transfered over network, one will need a common representation for all of the basic types.
Also, we'd want a way to see the stored values in the Unity editor's inspector.

So this is an attempt at achieving that.
I don't really need this functionality currently, so I just abandoned it.

The repository includes platform-independent binary serialization and deserialization of standard IEEE-whatever floats, which would be required for the aforementioned task.

The project may serve as a starting point to a person with similar goals in mind. 
