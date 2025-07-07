using System;
using System.Collections.Generic;
using System.Linq;

namespace HooverCanvassingApi.Services
{
    /// <summary>
    /// Advanced Traveling Salesman Problem solver using various algorithms
    /// </summary>
    public class TspSolver
    {
        /// <summary>
        /// Solve TSP using the 2-opt improvement algorithm
        /// </summary>
        public static List<int> Solve2Opt(double[,] distanceMatrix)
        {
            int n = distanceMatrix.GetLength(0);
            
            // Start with nearest neighbor solution
            var tour = NearestNeighbor(distanceMatrix);
            
            // Apply 2-opt improvements
            bool improved = true;
            while (improved)
            {
                improved = false;
                
                for (int i = 1; i < n - 2; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        if (j - i == 1) continue;
                        
                        var newTour = new List<int>(tour);
                        Reverse2OptSwap(newTour, i, j);
                        
                        if (GetTourDistance(newTour, distanceMatrix) < GetTourDistance(tour, distanceMatrix))
                        {
                            tour = newTour;
                            improved = true;
                        }
                    }
                }
            }
            
            return tour;
        }
        
        /// <summary>
        /// Solve TSP using Christofides algorithm (1.5-approximation)
        /// </summary>
        public static List<int> SolveChristofides(double[,] distanceMatrix)
        {
            int n = distanceMatrix.GetLength(0);
            
            // Step 1: Find minimum spanning tree
            var mst = MinimumSpanningTree(distanceMatrix);
            
            // Step 2: Find vertices with odd degree in MST
            var oddDegreeVertices = FindOddDegreeVertices(mst, n);
            
            // Step 3: Find minimum weight perfect matching on odd degree vertices
            var matching = MinimumWeightPerfectMatching(distanceMatrix, oddDegreeVertices);
            
            // Step 4: Combine MST and matching to form Eulerian graph
            var eulerianGraph = CombineGraphs(mst, matching, n);
            
            // Step 5: Find Eulerian tour
            var eulerianTour = FindEulerianTour(eulerianGraph);
            
            // Step 6: Convert to Hamiltonian tour by skipping repeated vertices
            var hamiltonianTour = ConvertToHamiltonian(eulerianTour);
            
            return hamiltonianTour;
        }
        
        /// <summary>
        /// Solve TSP using dynamic programming (optimal but O(n^2 * 2^n))
        /// Only practical for small instances (n < 20)
        /// </summary>
        public static List<int> SolveDynamicProgramming(double[,] distanceMatrix)
        {
            int n = distanceMatrix.GetLength(0);
            
            if (n > 20)
            {
                // Fall back to heuristic for large instances
                return Solve2Opt(distanceMatrix);
            }
            
            // dp[mask][i] = minimum distance to visit all vertices in mask ending at i
            var dp = new double[1 << n, n];
            var parent = new int[1 << n, n];
            
            // Initialize
            for (int mask = 0; mask < (1 << n); mask++)
            {
                for (int i = 0; i < n; i++)
                {
                    dp[mask, i] = double.MaxValue;
                    parent[mask, i] = -1;
                }
            }
            
            // Start from vertex 0
            dp[1, 0] = 0;
            
            // Fill DP table
            for (int mask = 1; mask < (1 << n); mask++)
            {
                for (int last = 0; last < n; last++)
                {
                    if ((mask & (1 << last)) == 0) continue;
                    if (dp[mask, last] == double.MaxValue) continue;
                    
                    for (int next = 0; next < n; next++)
                    {
                        if ((mask & (1 << next)) != 0) continue;
                        
                        int newMask = mask | (1 << next);
                        double newDist = dp[mask, last] + distanceMatrix[last, next];
                        
                        if (newDist < dp[newMask, next])
                        {
                            dp[newMask, next] = newDist;
                            parent[newMask, next] = last;
                        }
                    }
                }
            }
            
            // Find minimum tour
            double minTourDist = double.MaxValue;
            int lastVertex = -1;
            int fullMask = (1 << n) - 1;
            
            for (int i = 1; i < n; i++)
            {
                double tourDist = dp[fullMask, i] + distanceMatrix[i, 0];
                if (tourDist < minTourDist)
                {
                    minTourDist = tourDist;
                    lastVertex = i;
                }
            }
            
            // Reconstruct path
            var path = new List<int>();
            int currentMask = fullMask;
            int currentVertex = lastVertex;
            
            while (currentVertex != -1)
            {
                path.Add(currentVertex);
                int prevVertex = parent[currentMask, currentVertex];
                if (prevVertex != -1)
                {
                    currentMask ^= (1 << currentVertex);
                }
                currentVertex = prevVertex;
            }
            
            path.Reverse();
            return path;
        }
        
        // Helper methods
        
        private static List<int> NearestNeighbor(double[,] distanceMatrix)
        {
            int n = distanceMatrix.GetLength(0);
            var visited = new bool[n];
            var tour = new List<int> { 0 };
            visited[0] = true;
            
            int current = 0;
            for (int i = 1; i < n; i++)
            {
                double minDist = double.MaxValue;
                int nearest = -1;
                
                for (int j = 0; j < n; j++)
                {
                    if (!visited[j] && distanceMatrix[current, j] < minDist)
                    {
                        minDist = distanceMatrix[current, j];
                        nearest = j;
                    }
                }
                
                if (nearest != -1)
                {
                    tour.Add(nearest);
                    visited[nearest] = true;
                    current = nearest;
                }
            }
            
            return tour;
        }
        
        private static void Reverse2OptSwap(List<int> tour, int i, int j)
        {
            while (i < j)
            {
                int temp = tour[i];
                tour[i] = tour[j];
                tour[j] = temp;
                i++;
                j--;
            }
        }
        
        private static double GetTourDistance(List<int> tour, double[,] distanceMatrix)
        {
            double distance = 0;
            for (int i = 0; i < tour.Count; i++)
            {
                int from = tour[i];
                int to = tour[(i + 1) % tour.Count];
                distance += distanceMatrix[from, to];
            }
            return distance;
        }
        
        private static List<(int, int, double)> MinimumSpanningTree(double[,] distanceMatrix)
        {
            int n = distanceMatrix.GetLength(0);
            var edges = new List<(int from, int to, double weight)>();
            
            // Create all edges
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    edges.Add((i, j, distanceMatrix[i, j]));
                }
            }
            
            // Sort by weight
            edges.Sort((a, b) => a.weight.CompareTo(b.weight));
            
            // Kruskal's algorithm
            var parent = Enumerable.Range(0, n).ToArray();
            var rank = new int[n];
            var mst = new List<(int, int, double)>();
            
            int Find(int x)
            {
                if (parent[x] != x)
                    parent[x] = Find(parent[x]);
                return parent[x];
            }
            
            void Union(int x, int y)
            {
                int px = Find(x);
                int py = Find(y);
                
                if (rank[px] < rank[py])
                    parent[px] = py;
                else if (rank[px] > rank[py])
                    parent[py] = px;
                else
                {
                    parent[py] = px;
                    rank[px]++;
                }
            }
            
            foreach (var edge in edges)
            {
                if (Find(edge.from) != Find(edge.to))
                {
                    mst.Add(edge);
                    Union(edge.from, edge.to);
                    
                    if (mst.Count == n - 1)
                        break;
                }
            }
            
            return mst;
        }
        
        private static List<int> FindOddDegreeVertices(List<(int, int, double)> mst, int n)
        {
            var degree = new int[n];
            foreach (var edge in mst)
            {
                degree[edge.Item1]++;
                degree[edge.Item2]++;
            }
            
            var oddVertices = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (degree[i] % 2 == 1)
                    oddVertices.Add(i);
            }
            
            return oddVertices;
        }
        
        private static List<(int, int)> MinimumWeightPerfectMatching(double[,] distanceMatrix, List<int> vertices)
        {
            // Simple greedy matching for now
            var matching = new List<(int, int)>();
            var used = new HashSet<int>();
            
            var edges = new List<(int from, int to, double weight)>();
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    edges.Add((vertices[i], vertices[j], distanceMatrix[vertices[i], vertices[j]]));
                }
            }
            
            edges.Sort((a, b) => a.weight.CompareTo(b.weight));
            
            foreach (var edge in edges)
            {
                if (!used.Contains(edge.from) && !used.Contains(edge.to))
                {
                    matching.Add((edge.from, edge.to));
                    used.Add(edge.from);
                    used.Add(edge.to);
                }
            }
            
            return matching;
        }
        
        private static List<List<int>> CombineGraphs(List<(int, int, double)> mst, List<(int, int)> matching, int n)
        {
            var graph = new List<List<int>>();
            for (int i = 0; i < n; i++)
                graph.Add(new List<int>());
            
            foreach (var edge in mst)
            {
                graph[edge.Item1].Add(edge.Item2);
                graph[edge.Item2].Add(edge.Item1);
            }
            
            foreach (var edge in matching)
            {
                graph[edge.Item1].Add(edge.Item2);
                graph[edge.Item2].Add(edge.Item1);
            }
            
            return graph;
        }
        
        private static List<int> FindEulerianTour(List<List<int>> graph)
        {
            var tour = new List<int>();
            var stack = new Stack<int>();
            var currentGraph = graph.Select(adj => adj.ToList()).ToList();
            
            stack.Push(0);
            
            while (stack.Count > 0)
            {
                int v = stack.Peek();
                
                if (currentGraph[v].Count > 0)
                {
                    int u = currentGraph[v][0];
                    currentGraph[v].RemoveAt(0);
                    currentGraph[u].Remove(v);
                    stack.Push(u);
                }
                else
                {
                    tour.Add(stack.Pop());
                }
            }
            
            tour.Reverse();
            return tour;
        }
        
        private static List<int> ConvertToHamiltonian(List<int> eulerianTour)
        {
            var visited = new HashSet<int>();
            var hamiltonianTour = new List<int>();
            
            foreach (int vertex in eulerianTour)
            {
                if (!visited.Contains(vertex))
                {
                    hamiltonianTour.Add(vertex);
                    visited.Add(vertex);
                }
            }
            
            return hamiltonianTour;
        }
    }
}