using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Enemies.AI
{
    [Serializable]
    public struct PID {
   
        public float3 Kp;
        public float3 Ki;
        public float3 Kd;
   
        private float3 _outputMax;
        private float3 _outputMin;

        private float3 _preError;
   
        public float3 integral;
        private float3 _integralMax;
        private float3 _integralMin;
   
        public float3 output;
   
        public void SetBounds(float3 torque)
        {
            _outputMax = torque;
            _outputMin = -torque;
            _integralMax = Divide(_outputMax, Ki);
            _integralMin = Divide(_outputMin, Ki);       
        }
   
        public float3 Divide(float3 a, float3 b){
            Func<float, float> inv = (n) => 1/(n != 0? n : 1);
            var iVec = new float3(inv(b.x), inv(b.x), inv(b.z));
            return a * iVec;
        }
   
        public float3 Cycle(float3 pv, float3 setPoint, float dt){
            var error = setPoint - pv;
            integral = math.clamp(integral + (error * dt), _integralMin, _integralMax);
       
            var derivative = (error - _preError) / dt;
            output = Kp * error + Ki * integral + Kd * derivative;
            output = math.clamp(output, _outputMin, _outputMax);
       
            _preError = error;
            return output;
        }
    }
    
    public struct Thruster : IBufferElementData 
    {
        public float3 forward; // use the -forward axis as thrust dir
        public float maxThrust;
        public float lastThrust;
        public float3 Thrust => -lastThrust * forward;
        
        public float3 ApplyThrust(float3 idealForce)
        {
            float force = math.clamp(math.dot(idealForce, -forward), 0, maxThrust);
            lastThrust = force;
            return Thrust;
        }
    }
    
    public struct ThrusterPair : IBufferElementData 
    {
        public Thruster ThrusterA; //these will be on the primary/secondary plane, mirrored along the primary axis
        public Thruster ThrusterB;
        public float3 LocalAxisPrimary; // optimize force along this first
        public float3 LocalAxisSecondary; // optimize force along this second
        
        public float3 ApplyOptimalThrust(float3 idealForce)
        {
            float2 D1 = ToLocalPlane(-ThrusterA.forward);
            float2 D2 = ToLocalPlane(-ThrusterB.forward);
            float2 T = ToLocalPlane(idealForce);
            float2 forces = NumbersRunner(D1, D2, T, ThrusterA.maxThrust, ThrusterB.maxThrust);
            ThrusterA.lastThrust = forces.x;
            ThrusterB.lastThrust = forces.y;
            return ThrusterA.Thrust + ThrusterB.Thrust;
        }
        
        private float2 ToLocalPlane(float3 t)
        {
            return math.dot(t, LocalAxisPrimary) * new float2(1, 0) +
                   math.dot(t, LocalAxisSecondary) * new float2(0, 1);
        }
        
        public float2 NumbersRunner(float2 d1, float2 d2, float2 T, float m1, float m2)
        {
            // Step 1: Find solutions that minimize x-error
            float D1x = d1.x, D1y = d1.y;
            float D2x = d2.x, D2y = d2.y;
            float Tx = T.x, Ty = T.y;

            // Solve F1*D1x + F2*D2x = Tx
            float F1, F2;

            // Case 1: X-error can be zero
            if (math.abs(D1x) > 1e-6 || math.abs(D2x) > 1e-6)
            {
                // Parametrize the line F1*D1x + F2*D2x = Tx
                // Express F2 in terms of F1: F2 = (Tx - F1*D1x) / D2x
                bool useF1AsParam = math.abs(D2x) > 1e-6;
                float minParam = 0, maxParam = 0;

                if (useF1AsParam)
                {
                    minParam = math.clamp((Tx - m2 * D2x) / D1x, 0, m1);
                    maxParam = math.clamp(Tx / D1x, 0, m1);
                }
                else
                {
                    // If D1x is non-zero, express F1 in terms of F2
                    minParam = math.clamp((Tx - m1 * D1x) / D2x, 0, m2);
                    maxParam = math.clamp(Tx / D2x, 0, m2);
                }

                // Find the range of F1 (or F2) that satisfies constraints
                float paramStart = math.min(minParam, maxParam);
                float paramEnd = math.max(minParam, maxParam);

                // Evaluate y-error at endpoints
                float F1_start, F2_start, F1_end, F2_end;
                if (useF1AsParam)
                {
                    F1_start = paramStart;
                    F2_start = (Tx - F1_start * D1x) / D2x;
                    F1_end = paramEnd;
                    F2_end = (Tx - F1_end * D1x) / D2x;
                }
                else
                {
                    F2_start = paramStart;
                    F1_start = (Tx - F2_start * D2x) / D1x;
                    F2_end = paramEnd;
                    F1_end = (Tx - F2_end * D2x) / D1x;
                }

                // Clamp to ensure constraints
                F1_start = math.clamp(F1_start, 0, m1);
                F2_start = math.clamp(F2_start, 0, m2);
                F1_end = math.clamp(F1_end, 0, m1);
                F2_end = math.clamp(F2_end, 0, m2);

                // Calculate y-errors
                float errorY_start = math.abs(F1_start * D1y + F2_start * D2y - Ty);
                float errorY_end = math.abs(F1_end * D1y + F2_end * D2y - Ty);

                // Choose the endpoint with minimal y-error
                if (errorY_start <= errorY_end)
                {
                    F1 = F1_start;
                    F2 = F2_start;
                }
                else
                {
                    F1 = F1_end;
                    F2 = F2_end;
                }
            }
            else
            {
                // Edge case: D1x and D2x are both zero
                F1 = 0;
                F2 = 0;
            }
            return new float2(F1, F2);
        }
    }
}