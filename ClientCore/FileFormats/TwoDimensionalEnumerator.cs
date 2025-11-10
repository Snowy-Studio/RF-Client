using System.Collections;
using System.Collections.Generic;

namespace DTAConfig.Entity
{
    public class TwoDimensionalEnumerator<T> : IEnumerator<T>
    {
        T[,] array;
        int curX, curY;
        public TwoDimensionalEnumerator(T[,] array)
        {
            this.array = array;
            Reset();
        }
        public bool MoveNext()
        {
            curX++;
            if (curX == array.GetLength(0))
            {
                curX = 0;
                curY++;
            }
            return curY < array.GetLength(1);
        }
        public void Reset()
        {
            curX = -1;
            curY = 0;
        }
        T IEnumerator<T>.Current
        {
            get
            {
                return array[curX, curY];
            }
        }
        object IEnumerator.Current
        {
            get { return array[curX, curY]; }
        }
        public void Dispose() { }

    }
}
