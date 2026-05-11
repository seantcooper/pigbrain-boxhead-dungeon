using UnityEngine;

namespace pigbrain.core.Collections
{
    public class Group3<T>
    {
        public T x;
        public T y;
        public T z;

        public T this[int index]
        {
            get => index switch { 0 => x, 1 => y, 2 => z, _ => throw new($"Group3:OOB {index}"), };
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: throw new($"Group3:OOB {index}");
                }
            }
        }

    }
}