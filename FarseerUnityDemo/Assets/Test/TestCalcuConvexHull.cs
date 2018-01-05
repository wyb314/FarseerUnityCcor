using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using FarseerPhysics.Common;
using FarseerPhysics.Common.ConvexHull;
using Microsoft.Xna.Framework;
using UnityEngine;

public class TestCalcuConvexHull : MonoBehaviour
{


    public List<FVector2> pointCloud = new List<FVector2>();

    public int pointDensity = 100;
    public int raidus = 10;

    

    void OnGUI()
    {
        if (GUILayout.Button("Calculate convex"))
        {
            List<FVector2> list = new List<FVector2>(100);
            for (int i = 0; i < pointDensity; i++)
            {
                
                FVector2 point = new FVector2(
                    UnityEngine.Random.Range(-raidus, raidus),
                    UnityEngine.Random.Range(-raidus, raidus));

                list.Add(point);

            }

            this.InputVertices = new Vertices(list);

            this.StartCoroutine(this.GetConvexHullOneStep(this.InputVertices));
            //Vertices verticles = GiftWrap.GetConvexHull(new Vertices(list));

            //resultPointList.Clear();
            //foreach (var point in verticles)
            //{
            //    resultPointList.Add(point);
            //}

        }

    }

    public float depth = 10;

    void Update()
    {
        if (this.calculateCompleted)
        {
            int count = this.resultPointList.Count;

            for (int i = 0; i < count; i++)
            {
                FVector2 fPoint0 = this.resultPointList[i];
                FVector2 fPoint1 = this.resultPointList[(i + 1 == count) ? 0 : (i + 1)];

                Vector3 a = new Vector3(fPoint0.X, fPoint0.Y, depth);

                Vector3 b = new Vector3(fPoint1.X, fPoint1.Y, depth);

                Debug.DrawLine(a, b, Color.green);
            }

        }
        else
        {
            int count = this.convexingPointList.Count;

            for (int i = 0; i < count; i++)
            {
                FVector2 fPoint0 = this.convexingPointList[i];
                FVector2 fPoint1 = this.convexingPointList[(i + 1 == count) ? 0 : (i + 1)];

                Vector3 a = new Vector3(fPoint0.X, fPoint0.Y, depth);

                Vector3 b = new Vector3(fPoint1.X, fPoint1.Y, depth);

                Debug.DrawLine(a, b, Color.red);
            }

            Vector3 extremePoint = new Vector3(this.curConvexExtremePoint.X, this.curConvexExtremePoint.Y,depth);
            Vector3 lastTestPoint = new Vector3(this.curLastTestConvexExtremePoint.X, this.curLastTestConvexExtremePoint.Y, depth);
            Vector3 curTestPoint = new Vector3(this.curTestConvexExtremePoint.X, this.curTestConvexExtremePoint.Y, depth);

            Debug.DrawLine(extremePoint, lastTestPoint, Color.black);
            Debug.DrawLine(extremePoint, curTestPoint, Color.blue);
        }

        

        

    }



    public float waitSeconds = 0.25f;

    private int[] convexHull = null;

    private bool calculateCompleted = false;

    public Vertices InputVertices;
    public List<FVector2> resultPointList = new List<FVector2>();
    public List<FVector2> convexingPointList = new List<FVector2>();

    public FVector2 curConvexExtremePoint;

    public FVector2 curLastTestConvexExtremePoint;

    public FVector2 curTestConvexExtremePoint;

    public IEnumerator GetConvexHullOneStep(Vertices vertices)
    {
        this.calculateCompleted = false;
        // Find the right most point on the hull
        int i0 = 0;
        float x0 = vertices[0].X;
        for (int i = 1; i < vertices.Count; ++i)
        {
            float x = vertices[i].X;
            if (x > x0 || (x == x0 && vertices[i].Y < vertices[i0].Y))
            {
                i0 = i;
                x0 = x;
            }
        }

        int[] hull = new int[vertices.Count];
        convexHull = hull;
        int m = 0;
        int ih = i0;
        UnityEngine.Debug.LogError("i0->" + i0);
        this.convexingPointList.Clear();
        this.convexingPointList.Add(vertices[hull[ih]]);
        for (;;)
        {
            ConvexStepInput input = new ConvexStepInput()
            {
                hull =  hull,
                m = m,
                ih = ih,
                vertices = vertices,
                i0 = i0,
                result = false,
            };
            
            yield return CalculateConvexHullOneStep(input);

            UnityEngine.Debug.LogError("One xunhuan!m->" + input.m);

            hull = input.hull;
            m = input.m;
            ih = input.ih;
            vertices = input.vertices;
            

            if (input.result)
            {
                this.calculateCompleted = true;
                Debug.LogError("Calculate completed!");
                break;
            }
            this.convexingPointList.Clear();
            for (int i = 0; i < m ; ++i)
            {
                convexingPointList.Add(vertices[hull[i]]);
            }
            convexingPointList.Add(vertices[ih]);
        }


        
        Vertices result = new Vertices();

        // Copy vertices.
        for (int i = 0; i < m; ++i)
        {
            result.Add(vertices[hull[i]]);
        }

        resultPointList.Clear();
        foreach (var point in result)
        {
            resultPointList.Add(point);
        }
        //return result;
    }


    public Vertices GetConvexHull(Vertices vertices)
    {
        // Find the right most point on the hull
        int i0 = 0;
        float x0 = vertices[0].X;
        for (int i = 1; i < vertices.Count; ++i)
        {
            float x = vertices[i].X;
            if (x > x0 || (x == x0 && vertices[i].Y < vertices[i0].Y))
            {
                i0 = i;
                x0 = x;
            }
        }

        int[] hull = new int[vertices.Count];
        convexHull = hull;
        int m = 0;
        int ih = i0;

        for (;;)
        {
            hull[m] = ih;

            int ie = 0;
            for (int j = 1; j < vertices.Count; ++j)
            {
                if (ie == ih)
                {
                    ie = j;
                    continue;
                }

                FVector2 r = vertices[ie] - vertices[hull[m]];
                FVector2 v = vertices[j] - vertices[hull[m]];
                float c = MathUtils.Cross(r, v);
                if (c < 0.0f)
                {
                    ie = j;
                }

                // Collinearity check
                if (c == 0.0f && v.LengthSquared() > r.LengthSquared())
                {
                    ie = j;
                }
            }

            ++m;
            ih = ie;

            if (ie == i0)
            {
                break;
            }
        }

        Vertices result = new Vertices();

        // Copy vertices.
        for (int i = 0; i < m; ++i)
        {
            result.Add(vertices[hull[i]]);
        }
        return result;
    }


    public class ConvexStepInput
    {
        public int[] hull;
        public int m;
        public int ih;
        public Vertices vertices;
        public int i0;
        public bool result;
    }

    private IEnumerator CalculateConvexHullOneStep(ConvexStepInput input)
    {
        input.hull[input.m] = input.ih;
        UnityEngine.Debug.LogError("CalculateConvexHullOneStep m->" + input.m);
        int ie = 0;
        for (int j = 1; j < input.vertices.Count; ++j)
        {
            if (ie == input.ih)
            {
                ie = j;
                continue;
            }

            this.curConvexExtremePoint = input.vertices[input.hull[input.m]];
            this.curLastTestConvexExtremePoint = input.vertices[ie];
            this.curTestConvexExtremePoint = input.vertices[j];
            FVector2 r = input.vertices[ie] - input.vertices[input.hull[input.m]];
            FVector2 v = input.vertices[j] - input.vertices[input.hull[input.m]];
            float c = MathUtils.Cross(r, v);
            if (c < 0.0f)
            {
                ie = j;
            }

            // Collinearity check
            if (c == 0.0f && v.LengthSquared() > r.LengthSquared())
            {
                ie = j;
            }

            yield return new WaitForSeconds(waitSeconds);
        }

        ++input.m;
        input.ih = ie;

        if (ie == input.i0)
        {
            input.result = true;
        }
        else
        {
            input.result = false;
        }
        yield return new WaitForSeconds(0.001f);
    }


}
