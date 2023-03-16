using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Copy : ICloneable
{

    public static T[,] DeepCopy<T>(T[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);

        T[,] copy = new T[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (original[i, j] is ICloneable)
                {
                    copy[i, j] = (T)(original[i, j] as ICloneable).Clone();
                }
                else if (original[i, j] != null)
                {
                    throw new ArgumentException("The object must implement ICloneable interface.");
                }
            }
        }

        return copy;
    }

    public object Clone()
    {
        throw new NotImplementedException();
    }
}
