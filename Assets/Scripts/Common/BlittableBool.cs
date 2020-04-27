namespace Voxels.Common
{
    /*
        Most data types have a common representation in both managed and unmanaged memory 
        and do not require special handling by the interop marshaler.
        These types are called blittable types because they do not require conversion when passed between managed and unmanaged code.

        These types, for example, are blittable types:
            - Byte, Int16, Int32, Int64 

        But not Boolean. Bool is a non-blittable type and it converts to a 1, 2, or 4-byte value with true as 1 or -1.

        The following complex types are also blittable types:
            - One-dimensional arrays of blittable types, such as an array of integers.
            - Formatted value types that contain only blittable types (and classes if they are marshaled as formatted types).

        As an optimization, arrays of blittable types and classes containing only blittable members are pinned instead of copied during marshaling.
        These types can appear to be marshaled as In/Out parameters when the caller and callee are in the same apartment.
        However, these types are actually marshaled as In parameters and you must apply the InAttribute and OutAttribute attributes 
        if you want to marshal the argument as an In/Out parameter.

        Non-blittable types have different or ambiguous representations in managed and unmanaged languages.
        These types might require conversion when they are marshaled between managed and unmanaged code.
        For example, managed strings are non-blittable types because they can have several different unmanaged representations, 
        some of which can require conversion.
    */

    // to gain some optimization we can write our own blittable bool as a wraper around byte type.
    public struct BlittableBool
    {
        readonly byte _value;

        public BlittableBool(bool value)
        {
            _value = (byte)(value ? 1 : 0);
        }

        /* === implicit keyword ===
            The implicit keyword is used to declare an implicit user-defined type conversion operator.
            Use it to enable implicit conversions between a user-defined type and another type, 
            if the conversion is guaranteed not to cause a loss of data.
            Use the operator keyword to overload a built-in operator or to provide a user-defined conversion in a class or struct declaration.
        */

        /* === operator keyword ===
            To overload an operator on a custom class or struct, you create an operator declaration in the corresponding type.
            The operator declaration that overloads a built-in C# operator must satisfy the following rules:
                - It includes both a public and a static modifier.
                - It includes operator X where X is the name or symbol of the operator being overloaded.
                - Unary operators have one parameter, and binary operators have two parameters. 
                    In each case, at least one parameter must be the same type as the class or struct that declares the operator.
        */

        /* === explicit keyword ===
            The explicit keyword declares a user-defined type conversion operator that must be invoked with a cast.
        */

        public static implicit operator BlittableBool(bool value) => new BlittableBool(value);
        public static implicit operator bool(BlittableBool value) => value._value != 0;
    }

}
