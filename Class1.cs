namespace RandomPath
{
    public static class RandomPathExtensions
    {
        /// <summary>
        /// This is a simple complete path generation algorithm, that differs from the Durstenfeld method from the fact that this one
        /// doesn't account for constraints in the margins. It only guarantees that there are no duplicates in a single subpath.
        /// It is possible (by changing the call from the DurstenfeldPermutation method to the MakeMarginsParityConsistent
        /// method) to add the constraint of margins being parity consistent. This allows for parallelization of path calculation
        /// without any preceding margin calculation, while not violating any constraints. The path is not as random as possible
        /// without any constraint violations however.
        /// </summary>
        /// <param name="nodes">Amount of nodes.</param>
        /// <param name="length">The length of a subpath.</param>
        /// <param name="marginLength">In principle not needed for this method, but with the possibly added constraint of parity
        /// consistence of margins, the length of a margin is needed.</param>
        /// <returns>Returns a path of length (nodes * length). Possibly with parity consistent margins.</returns>
        public static int[] CalculatePathNoMargins(int nodes, int length, int marginLength)
        {
            int[] path = new int[nodes * length];
            bool firstNode = true;
            bool lastNode = false;
            for (int i = 0; i < nodes; i++)
            {
                if (i == 1) { firstNode = false; }
                if (i == nodes - 1) { lastNode = true; }
                int[] subPath = Program.DurstenfeldPermutation(length);
                //int[] subPath = new int[8] { 4, 1, 0, 7, 3, 6, 5, 2 };
                //subPath = MakeMarginsParityConsistent(subPath, marginLength, firstNode, lastNode);
                for (int j = 0; j < length; j++)
                {
                    path[i * length + j] = subPath[j];
                }
            }
            return path;
        }

        /// <summary>
        /// (Better version of the ParityPermutator)
        /// This method swaps all the odd values in the front (left) margin with the even values in the back (right) margin. If one
        /// margin is completely consistent of parity and the other margin is not, remaining values with wrong parity will be swapped
        /// with values from in between the margins, until both margins are completely parity consistent.
        /// </summary>
        /// <param name="path">Some path.</param>
        /// <param name="marginLength">The length of the margin (each node (except first and last) has two margins).</param>
        /// <param name="firstNode">A flag whether or not this is the path for the very first node, as that node doesn't need its first (left) margin to be parity consistent. (as there is no neighbouring node at that side).</param>
        /// <param name="lastNode">Same as firstNode but for the last node.</param>
        /// <returns>A complete path with parity consistent margins.</returns>
        public static int[] MakeMarginsParityConsistent(int[] path, int marginLength, bool firstNode, bool lastNode)
        {
            //Each frontmargin (left margin) is even, each backmargin (right margin) is odd

            //Don't forget that first and last path don't need their outer margin to be changed

            int frontIndex = 0;
            int backIndex = path.Length - marginLength;

            bool backFinished = false;
            if (!firstNode & !lastNode)
            {
                while (frontIndex < marginLength) //front is not yet finished
                {
                    if (backIndex < path.Length) //back is also not yet finished
                    {
                        if (path[frontIndex] % 2 == 1) //value in the frontMargin is odd and needs to be swapped
                        {
                            while (backIndex < path.Length) //back is not finished
                            {
                                if (path[backIndex] % 2 == 0) //value in the backMargin is even and needs to be swapped
                                {
                                    (path[frontIndex], path[backIndex]) = (path[backIndex], path[frontIndex]); //swap
                                    backIndex++;
                                    break;
                                }
                                backIndex++; //increase backIndex and try again 
                            }
                        }
                        if (backIndex >= path.Length) //If this statement is reached directly after a swap, frontIndex lags 1 behind. Shouldn't be a problem
                        {
                            backFinished = true;
                            break;
                        }

                        else
                        {
                            frontIndex++;
                        }
                    }
                    else
                    {
                        backFinished = true;
                        break;
                    }
                }
            }

            if (firstNode) { backFinished = false; }
            if (lastNode) { backFinished = true; }
            if (backFinished) //front needs to be finished as well
            {
                for (int i = frontIndex; i < marginLength; i++)
                {
                    if (path[i] % 2 == 1)
                    {
                        int pickedIndex;
                        if (!lastNode) {pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length - marginLength); }
                        else {pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length); }
                        while (path[pickedIndex] % 2 == 1) //value is odd
                        {
                            if (!lastNode) { pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length - marginLength); }
                            else { pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length); }
                        }
                        (path[i], path[pickedIndex]) = (path[pickedIndex], path[i]);
                    }
                }
            }

            else //front is finished
            {
                for (int i = backIndex; i < path.Length; i++)
                {
                    if (path[i] % 2 == 0)
                    {
                        int pickedIndex;
                        if (!firstNode) {pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length - marginLength); }
                        else {pickedIndex = Program.Globals.rnd.Next(0, path.Length - marginLength); }
                        while (path[pickedIndex] % 2 == 0) //value is even
                        {
                            if (!firstNode) { pickedIndex = Program.Globals.rnd.Next(marginLength, path.Length - marginLength); }
                            else { pickedIndex = Program.Globals.rnd.Next(0, path.Length - marginLength); }
                        }
                        (path[i], path[pickedIndex]) = (path[pickedIndex], path[i]);
                    }
                }
            }
            //diagnostics
/*            bool parityFlag = false;
            for (int i = 0; i < marginLength; i++)
            {
                if (path[i] % 2 == 1)
                {
                    parityFlag = true;
                }
            }
            for (int i = path.Length - marginLength; i < path.Length; i++)
            {
                if (path[i] % 2 == 0)
                {
                    parityFlag = true;
                }
            }
            if (firstNode | lastNode) { parityFlag = false; }
            Console.WriteLine(parityFlag);
*/
            return path;
        }

        /// <summary>
        /// This method allows for parallelization of the path generation, by generating two paths that both are half the size of
        /// the path that is needed. A coinflip decides whether the even values go on the even or odd indices, with the odd values
        /// taking the remaining indices. This results in a path with a higher distance to the mean, as a priori all values have a
        /// chance of 0 for half of the indices. With enough iterations however, this effect evens out because of the coinflip.
        /// </summary>
        /// <param name="length">The length of the path that is to be randomly permutated.</param>
        /// <returns>A randomly permutated path.</returns>
        public static int[] ParityPermutator(int length)
        {
            int[] subPermutation1 = Program.DurstenfeldPermutation(length / 2);
            int[] subPermutation2 = Program.DurstenfeldPermutation(length / 2);
            int[] path = new int[length];
            bool method = true;
            if (Program.Globals.rnd.Next(2) == 0)
            {
                method = false;
            };

            for (int i = 0; i < length / 2; i++)
            {
                if (method == false)
                {
                    path[2 * i] = subPermutation1[i] * 2;
                    path[2 * i + 1] = subPermutation2[i] * 2 + 1;
                }
                else
                {
                    path[2 * i + 1] = subPermutation1[i] * 2;
                    path[2 * i] = subPermutation2[i] * 2 + 1;
                }
            }
            /*            Console.WriteLine(string.Join(", ", combinedValues));
            */
            return path;
        }

        /// <summary>
        /// This method calculates a complete path, by calculating all the margins, then having the rest of each subpath calculated, and
        /// at the end stitching all the subpaths together to form the complete path. 
        /// </summary>
        /// <param name="nodes">The amount of nodes.</param>
        /// <param name="length">The size of the path per node.</param>
        /// <param name="marginLength">The length of the margin.</param>
        /// <returns>The complete path with no constraint violations (no duplicates in the path of a single node or in adjacent margins).</returns>
        public static int[] CalculateFullPath(int nodes, int length, int marginLength)
        {
            int[] path = new int[nodes * length];
            int[] margins = PermutateMarginsSequentialOrder(nodes, length, marginLength);
            //int[] margins = PermutateMarginsRandomOrder(nodes, length, marginLength);
            int[] tempSubPath;

            for (int i = 0; i < nodes; i++)
            {
                if (i == 0) //first node with only a margin at the end
                {
                    tempSubPath = PermutationSupplementor(length, Array.Empty<int>(), margins[..marginLength]);
                }
                else if (i == nodes - 1) //last node with only a margin at the beginning
                {
                    int[] tempFrontMargin2 = margins[((2 * i - 1) * marginLength)..(2 * i * marginLength)];
                    tempSubPath = PermutationSupplementor(length, tempFrontMargin2, Array.Empty<int>());
                }
                else //regular nodes with margins at both sides
                {
                    int[] tempFrontMargin = margins[((2 * i - 1) * marginLength)..(2 * i * marginLength)];
                    int[] tempBackMargin = margins[(2 * i * marginLength)..((2 * i + 1) * marginLength)];
                    tempSubPath = PermutationSupplementor(length, tempFrontMargin, tempBackMargin);
                }

                int index = 0;
                for (int j = i * length; j < ((i + 1) * length); j++)
                {
                    path[j] = tempSubPath[index];
                    index++;
                }
            }
            return path;
        }

        /// <summary>
        /// This function fills the margins of a complete path for a certain amount of nodes. It does so in a random order, keeping
        /// track of the remaining set of values that is allowed for some margin.
        /// </summary>
        /// <param name="nodes">The amount of nodes.</param>
        /// <param name="length">The size of the subpath.</param>
        /// <param name="marginLength">The length of the margin.</param>
        /// <returns>All the margins concatenated in a single array.</returns>
        private static int[] PermutateMarginsRandomOrder(int nodes, int length, int marginLength)
        {
            int marginCount = nodes * 2 - 2;
            int[] marginArray = new int[marginCount * marginLength];
            int[] marginArrayIndexes = new int[marginCount];

            HashSet<int> availableMargins = new();
            for (int i = 0; i < marginCount; i++)
            {
                marginArrayIndexes[i] = i * marginLength;
                availableMargins.Add(i);
            }

            List<HashSet<int>> availableValues = new();
            for (int i = 0; i < marginCount; i++)
            {
                HashSet<int> temp = new();
                for (int j = 0; j < length; j++)
                {
                    temp.Add(j);
                }
                availableValues.Add(temp);
            }

            while (availableMargins.Count > 0)
            {
                int currentMargin = availableMargins.ElementAt(Program.Globals.rnd.Next(0, availableMargins.Count));
                int pickedValue = availableValues[currentMargin].ElementAt(Program.Globals.rnd.Next(0, availableValues[currentMargin].Count));
                marginArray[marginArrayIndexes[currentMargin]] = pickedValue;
                marginArrayIndexes[currentMargin]++;
                if (marginArrayIndexes[currentMargin] % marginLength == 0)
                {
                    availableMargins.Remove(currentMargin);
                }

                availableValues[currentMargin].Remove(pickedValue);
                if (currentMargin > 0)
                {
                    availableValues[currentMargin - 1].Remove(pickedValue);
                }
                if (currentMargin != marginCount - 1)
                {
                    availableValues[currentMargin + 1].Remove(pickedValue);
                }
            }
            return marginArray;
        }
        /// <summary>
        /// This function fills the margins of a path for a certain amount of nodes. It does so in a sequential order, where
        /// it only has to keep track of the values in the preceding margin and in the margin it is currently filling to decide the set
        /// of remaining values that is allowed for the cell it is currently allocating a value.
        /// </summary>
        /// <param name="nodes">The amount of nodes.</param>
        /// <param name="length">The length of a subpath.</param>
        /// <param name="marginLength">The length of the margin.</param>
        /// <returns>All the margins concatenated in a single array.</returns>
        private static int[] PermutateMarginsSequentialOrder(int nodes, int length, int marginLength)
        {
            int marginCount = nodes * 2 - 2;
            int[] marginArray = new int[marginCount * marginLength];

            //Filling the first margin separately as it has no preceding margin
            List<int> tabuList = new();
            /*            I thought an empty Array is initialized with 0 as value for each index,
             *            and I didn't want to risk that a 0 as pickedValue could never be placed,
             *            because the `forbidden' is full of zeroes at the beginning.
            */
            for (int k = 0; k < marginLength; k++)
            {
                int pickedValue = Program.Globals.rnd.Next(0, length);
                while (tabuList.Contains(pickedValue))
                {
                    pickedValue = Program.Globals.rnd.Next(0, length);
                }
                marginArray[k] = pickedValue;
                tabuList.Add(pickedValue);
            }

            for (int i = 1; i < marginCount; i++)
            {
                for (int j = 0; j < marginLength; j++)
                {
                    int[] forbidden = marginArray[((i - 1) * marginLength)..(i * marginLength + j)]; //forbidden basically same as tabuList
                    int pickedValue = Program.Globals.rnd.Next(0, length);
                    while (Array.IndexOf(forbidden, pickedValue) > -1) //Will be false if pickedValue is not in forbidden
                    {
                        pickedValue = Program.Globals.rnd.Next(0, length);
                    }
                    marginArray[i * marginLength + j] = pickedValue;
                }
            }
            return marginArray;
        }

        /// <summary>
        /// This method takes the contents of the two margins of a subpath, and supplements the rest of the subpath with the remaining allowed
        /// values.
        /// </summary>
        /// <param name="length">The length of the subpath.</param>
        /// <param name="frontMargin">The contents of the front (left) margin.</param>
        /// <param name="backMargin">The contents of the back (right) margin.</param>
        /// <returns>A completely filled in path from a node.</returns>
        private static int[] PermutationSupplementor(int length, int[] frontMargin, int[] backMargin)
        {
            int[] randomPermutation = new int[length];
            HashSet<int> marginValues = new();
            /*At the same time, the values of the margins are put in a set,
             * and values in the margin are copied over to the array that is
             * to become the permutation that is returned.
            */
            for (int i = 0; i < frontMargin.Length; i++)
            {
                marginValues.Add(frontMargin[i]);
                randomPermutation[i] = frontMargin[i];
            }

            int tempIndex = randomPermutation.Length - 1;
            for (int i = 0; i < backMargin.Length; i++)
            {
                marginValues.Add(backMargin[i]);
                randomPermutation[tempIndex] = backMargin[i];
                tempIndex--;
            }

            int index = frontMargin.Length;
            for (int value = 0; value < length; value++)
            {
                if (marginValues.Contains(value) == false)
                {
                    randomPermutation[index] = value;
                    index++;
                }
            }

            //Now the sequence between frontMargin and backMargin gets shuffled using Durstenfeld algorithm
            for (int i = (length - backMargin.Length) - 1; i > (frontMargin.Length - 1); i--)
            {
                int j = Program.Globals.rnd.Next(frontMargin.Length, i + 1);
                (randomPermutation[i], randomPermutation[j]) = (randomPermutation[j], randomPermutation[i]);
            }
            return randomPermutation;
        }
    }
}
