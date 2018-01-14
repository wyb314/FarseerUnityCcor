using System.Collections;
using System.Collections.Generic;
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;
using UnityEngine;

public class TestGiftWarpAlgorithm : MonoBehaviour
{


    private List<FVector2> inputVerticles = new List<FVector2>();

    public ConvexHullStruct convexHull;

    //private List<int> convexHull = new List<int>();
    //public int headPoint = 0;
    //public int curBestExtreme = 0;
    //public int curTestPoint = 0;

    public int pointDensity = 100;
    public int raidus = 10;
    void OnGUI()
    {
        if (GUILayout.Button("Calculate convex"))
        {
            this.inputVerticles = new List<FVector2>(100);
            for (int i = 0; i < pointDensity; i++)
            {

                FVector2 point = new FVector2(
                    UnityEngine.Random.Range(-raidus, raidus),
                    UnityEngine.Random.Range(-raidus, raidus));

                inputVerticles.Add(point);

            }

            this.convexHull.Reset();
            this.StartCoroutine(this.CalculateConvexHull());
        }
    }

    [System.Serializable]
    public class ConvexHullStruct
    {
        public int convexHeadExtremePoint;

        public int potentialNextExtremePoint;

        public int curTestPoint;
        
        public List<int> convexHull = new List<int>();

        public bool calculateCompleted = false;
        public void Add(int extremaPoint)
        {
            this.convexHull.Add(extremaPoint);
        }

        public int Count
        {
            get { return this.convexHull.Count; }
        }

        public void Reset()
        {
            this.calculateCompleted = false;
            this.convexHull.Clear();
            this.convexHeadExtremePoint = 0;
            this.potentialNextExtremePoint = 0;
            this.curTestPoint = 0;
        }
    }

    private bool calculateStarted = false;
    public float zDepth = 5;
    void Update()
    {
        int count = this.convexHull.Count;

        if (count < 2)
        {
            return;
        }

        if (!this.convexHull.calculateCompleted)
        {
            Vector3 extremePoint = new Vector3(this.inputVerticles[this.convexHull.convexHeadExtremePoint].X,
            this.inputVerticles[this.convexHull.convexHeadExtremePoint].Y, zDepth);

            Vector3 curTestPoint = new Vector3(this.inputVerticles[this.convexHull.curTestPoint].X,
                this.inputVerticles[this.convexHull.curTestPoint].Y, zDepth);
            Vector3 curBestExtremePoint = new Vector3(this.inputVerticles[this.convexHull.potentialNextExtremePoint].X,
                this.inputVerticles[this.convexHull.potentialNextExtremePoint].Y, zDepth);

            Debug.DrawLine(extremePoint, curBestExtremePoint, Color.black);
            Debug.DrawLine(extremePoint, curTestPoint, Color.blue);
        }

        

        int upper = (!this.convexHull.calculateCompleted) ? count - 1 : count;
        for (int i = 0; i < upper; i++)
        {
            FVector2 fPoint0 = this.inputVerticles[this.convexHull.convexHull[i]];
            FVector2 fPoint1 = this.inputVerticles[this.convexHull.convexHull[(i + 1 == count) ? 0 : (i + 1)]];

            Vector3 a = new Vector3(fPoint0.X, fPoint0.Y, zDepth);

            Vector3 b = new Vector3(fPoint1.X, fPoint1.Y, zDepth);

            Debug.DrawLine(a, b, Color.green);
        }





    }


    private int CalculateFirstConvexHullPoint()
    {
        // Find the right most point on the hull
        int i0 = 0;
        float x0 = inputVerticles[0].X;
        for (int i = 1; i < inputVerticles.Count; ++i)
        {
            float x = inputVerticles[i].X;
            if (x > x0 || (x == x0 && inputVerticles[i].Y < inputVerticles[i0].Y))
            {
                i0 = i;
                x0 = x;
            }
        }
        this.convexHull.Add(i0);

        return i0;
    }

    public float delayInvokeIntervel = 0.03f;
    private IEnumerator CalculateConvexHull()
    {
        this.calculateStarted = true;
        int i0 = this.CalculateFirstConvexHullPoint();
        int n = this.inputVerticles.Count;
        this.convexHull.convexHeadExtremePoint = i0;
        while (true)
        {
            this.convexHull.potentialNextExtremePoint = 0;
            for (int i = 1; i < n; i++)
            {
                this.convexHull.curTestPoint = i;
                this.CalculateOneConvexHullExtremaPoint();
                yield return new WaitForSeconds(delayInvokeIntervel);
            }

            this.convexHull.convexHeadExtremePoint = this.convexHull.potentialNextExtremePoint;
           
            if (this.convexHull.convexHeadExtremePoint == i0)
            {
                break;
            }

            this.convexHull.Add(this.convexHull.convexHeadExtremePoint);
        }
        this.convexHull.calculateCompleted = true;

        Debug.LogError("Calculate ContexHull Completed!");
    }


    private void CalculateOneConvexHullExtremaPoint()
    {
        FVector2 r = this.inputVerticles[this.convexHull.potentialNextExtremePoint] - this.inputVerticles[this.convexHull.convexHeadExtremePoint];
        FVector2 v = this.inputVerticles[this.convexHull.curTestPoint] - this.inputVerticles[this.convexHull.convexHeadExtremePoint];

        float c = MathUtils.Cross(r, v);
        if (c < 0.0f)
        {
            this.convexHull.potentialNextExtremePoint = this.convexHull.curTestPoint;
        }

        if (c == 0.0f && v.LengthSquared() > r.LengthSquared())
        {
            this.convexHull.potentialNextExtremePoint = this.convexHull.curTestPoint;
        }

    }

    //private bool ToLeftTest()
    //{

    //}

}
