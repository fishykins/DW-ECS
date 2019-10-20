using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
namespace PlanetaryTerrain
{
    public static class QuadNeighbor
    {
        /*
        This is a hybrid approach to finding quad neighbors in quadtree planets.
        The (not yet spherified) planet is unfolded, the unfolded cube is projected onto a 2x2 quadtree that has been subdivided once.

        |00| "01" |10| |11| 
        "02" "03" "12" "13"
        |20| "21" |30| |31|
        |22| |23| |30| |31|

        The quads in quotes are used, the ones with vbars are empty. You should be able to see the familiar unfolded cube shape.
        Where the quads are directly next to a neighbor, the neighbor is found with the algorithm described in this paper: https://web.archive.org/web/20120907211934/http://ww1.ucmss.com/books/LFS/CSREA2006/MSV4517.pdf

        Because the cube is unfolded, there are edge neighbors, which aren't next to each other in the quadtree. Some of these, for example neighbor of 13 in direction 0 (right), quad 02, can be found using the same algorithm.
        For others, like neighbor of quad 21 in direction 2 (down), the algorithm would return unused or wrong quads. Those neighbors are found by replacing or inverting certain numbers in the location string according to the Dictonary quadEdgeNeighbors.


         */

        
        static readonly Dictionary<string, string> dict = new Dictionary<string, string>()
    {
        {"0/0", "1/h"}, // {"direction/quadID", "newQuadId/operation"}
        {"0/1", "0/0"},
        {"0/2", "3/h"},
        {"0/3", "2/0"},

        {"1/0", "1/1"},
        {"1/1", "0/h"},
        {"1/2", "3/1"},
        {"1/3", "2/h"},

        {"2/0", "2/h"},
        {"2/1", "3/h"},
        {"2/2", "0/2"},
        {"2/3", "1/2"},

        {"3/0", "2/3"},
        {"3/1", "3/3"},
        {"3/2", "0/h"},
        {"3/3", "1/h"},

    };

        static readonly Dictionary<string, string> quadEdgeNeighbors = new Dictionary<string, string>() //Right: 0; Left: 1; Down: 2; Up: 3;
    {
        {"3/12", "01/0/3"}, // {"direction/startQuadID", "new id/to replace/replace with"}
        {"0/01", "12/3/0"},

        {"3/02", "01/1/2"}, // in this case: when neighbors to quad 02 in direction 3 (up) are searched, replace the first two letters of the quadId with 01, then replace every 1 in the rest of the id with a 2
        {"1/01", "02/2/1"},

        {"3/13", "01/0/1"},
        {"3/01", "13/1/0"},


        {"2/02", "21/3/0"},
        {"1/21", "02/0/3"},

        {"2/12", "21/2/1"},
        {"0/21", "12/1/2"},

        {"2/13", "21/0/0"},
        {"2/21", "13/0/0"},

    };

        public static string GetNeighbor(string quadId, string direction)
        {
            char[] neighborId = quadId.ToCharArray();
            string quad = LastChar(quadId);

            if (quadEdgeNeighbors.ContainsKey(direction + "/" + FirstNChars(quadId, 2)))
            {
                if (AtQuadEdge(quadId, direction))
                {
                    return QuadEdges(quadId, direction);
                }
            }

            for (int i = 0; i < quadId.Length; i++)
            {
                string c = direction + "/" + quad;

                neighborId[quadId.Length - i - 1] = FirstNChars(dict[c], 1)[0];

                direction = LastChar(dict[c]);

                if (direction == "h" || (quadId.Length - i - 2) < 0)
                    break;
                else if (direction != "h")
                {
                    quad = new string(neighborId[quadId.Length - i - 2], 1);
                }

            }
            string neighbor = new string(neighborId);
            return neighbor;
        }

        public static string QuadEdges(string quadId, string direction)
        {

            string result = quadEdgeNeighbors[direction + "/" + FirstNChars(quadId, 2)];

            bool invert = (FirstNChars(quadId, 2) == "13" || FirstNChars(quadId, 2) == "01" && direction == "3"); //Characters need to be inverted instead of replaced in quads that start with 13 or 11 in dir 3

            string toReplace = result.Substring(3, 1);
            string replaceWith = LastChar(result);

            string newId = FirstNChars(result, 2);       //first two chars of new id are always equal
            char[] quadIdChars = quadId.ToCharArray();

            for (int i = 2; i < quadIdChars.Length; i++) //character specified in dictionary is replaced with another (or inverted) 
            {
                string c = new string(quadIdChars[i], 1);

                if (!invert)
                {
                    if (c == toReplace)
                        newId += replaceWith;
                    else
                        newId += c;
                }
                else
                {
                    if (c == toReplace)
                        newId += replaceWith;
                    else if (c == replaceWith)
                        newId += toReplace;
                }
            }
            return newId;
        }
        static bool AtQuadEdge(string quadId, string direction)
        {
            if (quadId.Length > 2)
            {
                quadId = quadId.Substring(2);

                switch (direction)
                {
                    case "0":
                        if (!quadId.Contains("0") && !quadId.Contains("2"))
                            return true;
                        else
                            return false;
                    case "1":
                        if (!quadId.Contains("1") && !quadId.Contains("3"))
                            return true;
                        else
                            return false;
                    case "2":
                        if (!quadId.Contains("0") && !quadId.Contains("1"))
                            return true;
                        else
                            return false;
                    case "3":
                        if (!quadId.Contains("2") && !quadId.Contains("3"))
                            return true;
                        else
                            return false;
                    default:
                        return false;
                }
            }
            else
                return true;
        }
        static string LastChar(string s)
        {
            return s.Substring(s.Length - 1, 1);
        }

        static string FirstNChars(string s, int n)
        {
            return s.Substring(0, n);
        }

    }

   
}
