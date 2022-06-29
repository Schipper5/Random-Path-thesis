using System.Globalization;

namespace RandomPath
{
    class Program
    {
        public static class Globals
        {
            public static Random rnd = new(5);
        }
        /// <summary>
        /// The parameters needed for complete path calculation are asked here.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
          /*  Console.WriteLine("type 0 for multiple nodes, type 1 for single node, type 2 for multiple nodes but no margins.");
            int method = int.Parse(Console.ReadLine());

            int nodes = 0;
            int marginLength = 0;
            if (method != 1)
            {
                Console.WriteLine("Amount of nodes?");
                nodes = int.Parse(Console.ReadLine());

                Console.WriteLine("What is the length of the margin? (maximum 1/3 of permutation length, and an even number)");
                marginLength = int.Parse(Console.ReadLine());
            }

            Console.WriteLine("What is the length of the permutation (per node)?");
            int length = int.Parse(Console.ReadLine());

            Console.WriteLine("Maximal amount of iterations?");
            int maxIterations = int.Parse(Console.ReadLine());

            Console.WriteLine("Delta?");
            int delta = int.Parse(Console.ReadLine());

            Console.WriteLine("Notes for this run?");
            string notes = Console.ReadLine();

            CalculateTrend(method, length, maxIterations, delta, notes, nodes, marginLength);
*/
            CalculateTrend(2, 100, 1000, 1, "Manhattan", 10, 10);
        }

        /// <summary>
        /// This method repeatedly calls TotalDistanceToMean to calculate the relation between (the total distance of the realized fraction of 
        /// times that some value is permuted in some location, and the theoretical fraction) and the amount of iterations. This relation
        /// will be dumped in a .csv file.</summary>
        /// <param name="method">User can choose different methods of path calculation.</param>
        /// <param name="length">Length of the array that is to be permutated. In case of multiple nodes, its the length of the array
        /// per node. So the total path length will be nodes * length.</param>
        /// <param name="maxIterations">TotalDistanceToMean will increase the amount of iterations for each run from 1 to this value.</param>
        /// <param name="delta">Stepsize that TotalDistanceToMean will increase iterations with.</param>
        /// <param name="notes">This string will be added at the end of the .csv file</param>
        /// <param name="nodes">Amount of nodes that will be used in path calculation. Not relevant for all path generation methods.</param>
        /// <param name="marginLength">Length of the margin of each subpath. Note that all subpaths (except the first and last) will have two
        /// margins in total.</param>
        static void CalculateTrend(int method, int length, int maxIterations, int delta, string notes, int nodes = 0, int marginLength = 0)
        {
            var records = new List<MeanDistTuple> { };

            for (int i = 1; i < maxIterations; i += delta)
            {
                //Console.WriteLine("i = " + i.ToString());

                double distance = TotalDistanceToMean(method, i, length, nodes, marginLength);
                //Console.WriteLine(distance.ToString());

                records.Add(new MeanDistTuple { Iterations = i, MeanDist = distance });
                if (((i - 1) * 10) % ((float)maxIterations / 10) == 0)
                {
                    Console.Write((int)(i / (float)maxIterations * 100) + "% ");
                }
            }
            using (var writer = new StreamWriter("../" + method.ToString() + "x" + length.ToString() + "x" + maxIterations.ToString() + "x" + delta.ToString() + "x" + nodes.ToString() + "x" + marginLength.ToString() + "x" + notes + ".csv"))
            using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
        }

        /// <summary>
        /// This method calculates some amount of paths, and makes a cartesian product of the set of values (a matrix with each value x location
        /// pair in it). It then calculates the total distance of each value x location pair to the theoretical fraction (the value that it should approach
        /// with the amount of iterations approaching infinity).
        /// </summary>
        /// <param name="method">How the path will be randomly permutated.</param>
        /// <param name="iterations">How many paths will be generated.</param>
        /// <param name="length">Length of the array that is to be permutated. In case of multiple nodes, its the length of the array
        /// per node (subpath). So the total path length will be nodes * length.</param>
        /// <param name="nodes">Amount of nodes that will be used in path calculation. Not relevant for all path generation methods.</param>
        /// <param name="marginLength">Length of the margin of each subpath. Note that all nodes (except first and last) will have two
        /// margins in total.</param>
        /// <returns>The total distance of (the count of each value x location pair / iteration count) to the theoretical fraction.</returns>
        static double TotalDistanceToMean(int method, int iterations, int length, int nodes, int marginLength)
        {
            //The countArray counts how many times some int is placed on some location
            int[,] countArray = new int[length, length];
            for (int i = 0; i < iterations; i++)
            {
                int[] randomPermutation = Array.Empty<int>();
                if (method == 0)
                {
                    randomPermutation = RandomPathExtensions.CalculateFullPath(nodes, length, marginLength);
                }

                else if (method == 1)
                {
                    randomPermutation = DurstenfeldPermutation(length);
                }

                else if (method == 2)
                {
                    randomPermutation = RandomPathExtensions.CalculatePathNoMargins(nodes, length, marginLength);
                }

                else if (method == 3)
                {
                    randomPermutation = RandomPathExtensions.ParityPermutator(length);
                }

                for (int j = 0; j < length; j++)
                {
                        int value = randomPermutation[j];
                        countArray[value, j]++;
                }
            }
            /*            Console.WriteLine("countArray");
						PrintMatrix(countArray);
			*/
            /*            PrintDistMean(countArray, iterations);
			*/
            double distSum = 0;
            float theoreticalFraction = 1 / (float)length;
            /*            Console.WriteLine("Theoretical mean:");
						Console.WriteLine(theoreticalMean.ToString());
					  float[,] percentageArray = new float[length, length];
			*/
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    float fraction = (float)countArray[i, j] / (float)iterations;
                    //double distance = Math.Pow(fraction - theoreticalFraction, 2); //Euclidian
                    double distance = Math.Abs(fraction - theoreticalFraction); //Manhattan
                    distSum += distance;
                }
            }
            //return Math.Sqrt(distSum);
            return distSum;
        }

        /// <summary>
        /// An implementation of the Durstenfeld algorithm to randomly permutate an array.
        /// </summary>
        /// <param name="length">The length of the array that is to be randomly permutated.</param>
        /// <returns>The permutation.</returns>
        public static int[] DurstenfeldPermutation(int length)
        {
            int[] randomPermutation = new int[length];
            for (int i = 0; i < length; i++)
            {
                randomPermutation[i] = i;
            }

            //An implementation of the Durstenfeld algorithm
            for (int i = length - 1; i > 0; i--)
            {
                //int j = RandomNumberGenerator.GetInt32(0, i + 1);
                int j = Globals.rnd.Next(0, i + 1);
                (randomPermutation[i], randomPermutation[j]) = (randomPermutation[j], randomPermutation[i]);
            }

            /*            Console.WriteLine("Random matrix:");
                        Console.WriteLine(string.Join(", ", randomArray));
            */
            return randomPermutation;
        }   

        static void PrintMatrix(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            Console.WriteLine("the value in the n'th row of the m'th column is the percentage of how many times the number n has been placed on index m");
            Console.Write(Environment.NewLine);
            Console.Write(Environment.NewLine);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(string.Format("{0} ", matrix[i, j]));
                }
                Console.Write(Environment.NewLine + Environment.NewLine);
            }
        }
        static void PrintDistMean(int[,] countMatrix, int iterations)
        {
            //CHECK FOR WRONG FLOATS
            int rows = countMatrix.GetLength(0);
            int cols = countMatrix.GetLength(1);
            Console.WriteLine("percentages:");
            Console.Write(Environment.NewLine);
            Console.Write(Environment.NewLine);
            float theoreticalMean = 1 / cols;

            float[,] percentagesMatrix = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    percentagesMatrix[i, j] = Math.Abs((countMatrix[i, j] / (float)iterations) - theoreticalMean);
                    Console.Write(string.Format("{0} ", percentagesMatrix[i, j].ToString()));

                }
                Console.Write(Environment.NewLine + Environment.NewLine);
            }
        }

        /// <summary>
        /// A tuple with the total distance to mean for some amount of iterations.
        /// </summary>
        public class MeanDistTuple
        {
            public int Iterations { get; set; }
            public double MeanDist { get; set; }
        }
    }
}
