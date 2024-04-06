using UnityEngine;
using System.Collections.Generic;
using LogicGateInjector;
using SFS.World;
using SFS.Variables;
using System;
using Object = UnityEngine.Object;
using SFS.Parts.Modules;
using System.Net.Sockets;

namespace LogicGateModules
{
    public abstract class CustomModuleBase : MonoBehaviour
    {
        public List<Object> vars = new List<Object>();
        public abstract int Id();

        public bool outValue = false;
        public void SetOutputs(bool value, float multiplier = 1f)
        {
            outValue = value;
            for (int i = 0; i < Mathf.Min(vars.Count, 4); i++) // by default, the first 4 variables should be outputs as GOs
            {
                if (vars[i] is GameObject go && go != null)
                {
                    go.transform.localPosition = new Vector3(go.transform.localPosition.x, value ? 0f : (0.1f * multiplier), go.transform.localPosition.z);
                }
            }
        }

        public void ToggleOutputs(float mult) => SetOutputs(!outValue, mult);
        public bool[] GetInputs()
        {
            List<bool> result = new List<bool>();
            for (int i = 0; i < vars.Count; i++)
            {
                var variable = vars[i];
                if (variable is InputTrigger it && variable != null)
                {
                    result.Add(it.enab);
                }
            }
            return result.ToArray();
        }
    }

    public class AltimeterModule : CustomModuleBase
    {
        // variables:
        // 0-3: output(s)
        // 4: VariablesModule

        Player player;
        public override int Id() => 1;

        public void Update()
        {
            try
            {
                player = PlayerController.main.player.Value;
                if (player != null)
                {
                    VariablesModule vm = GetComponentInChildren<VariablesModule>();
                    double height = player.location.Value.Height;

                    SetOutputs(height > vm.doubleVariables.GetVariable("minHeight").Value && height < vm.doubleVariables.GetVariable("maxHeight").Value);
                }
            }
            catch (Exception) { }
        }
    }

    public class LogicEngineModule : CustomModuleBase
    {
        public override int Id() => 2;
        public EngineModule em;
        public bool eo;

        public void Update()
        {
            em = GetComponent<EngineModule>();

            bool canRun = true;

            foreach (FlowModule.Flow flow in em.source.sources)
            {
                if (!(flow.state == FlowModule.FlowState.CanFlow || flow.state == FlowModule.FlowState.IsFlowing))
                {
                    canRun = false;
                    break;
                }
            }

            em.engineOn.Value = GetInputs()[0] && canRun;
        }
    }

    public class SignalDelayerModule : CustomModuleBase
    {
        public override int Id() => 3;

        bool inputState;

        float inputDelay;

        public void FixedUpdate()
        {
            inputDelay = (float) GetComponentInChildren<VariablesModule>()
                .doubleVariables
                .GetValue("delay");

            if (inputState != GetInputs()[0])
            {
                Invoke(nameof(ToggleState), inputDelay);
            }

            inputState = GetInputs()[0];
        }

        public void ToggleState()
        {
            ToggleOutputs(1f);
        }

    }
    public class WireLengthModule : CustomModuleBase
    {
        public override int Id() => 4;

        GameObject inputArrow;
        GameObject input;

        VariablesModule vm;

        float length;

        public void Start()
        {
            inputArrow = vars[1] as GameObject;
            input = vars[0] as GameObject;
            vm = GetComponent<VariablesModule>();
        }
        public void Update()
        {
            length = (float)vm.doubleVariables.GetValue("length");
            inputArrow.transform.localPosition = new Vector3(inputArrow.transform.localPosition.x, length - 0.2f, inputArrow.transform.localPosition.z);
            input.transform.localPosition = new Vector3(input.transform.localPosition.x, length, input.transform.localPosition.z);
            GetComponent<MagnetModule>().points[0].position.y.Value = length;

        }
    }

    public class AngleSensorModule : CustomModuleBase
    {
        public override int Id() => 5;

        Rocket rkt;
        VariablesModule vm;
        float minAng;
        float maxAng;
        float lastLoggedAngle;
        float logInterval = 5f; // Log interval in seconds
        float lastLogTime;

        public void Update()
        {
            try
            {
                rkt = PlayerController.main.player.Value as Rocket;
                vm = GetComponent<VariablesModule>();

                minAng = (float)vm.doubleVariables.GetValue("minAngle");
                maxAng = (float)vm.doubleVariables.GetValue("maxAngle");

                if (vm.boolVariables.GetValue("localAngle"))
                {
                    // Local rotation code by NeptuneSky
                    float globalAngle = NormalizeAngle(rkt.partHolder.transform.eulerAngles.z);
                    Location location = rkt.location.Value;

                    Vector2 orbitAngleVector = new Vector2(Mathf.Cos((float)location.position.AngleRadians), Mathf.Sin((float)location.position.AngleRadians)).Rotate_Radians(270 * Mathf.Deg2Rad);
                    var facing = new Vector2(Mathf.Cos(globalAngle * Mathf.Deg2Rad), Mathf.Sin(globalAngle * Mathf.Deg2Rad));
                    float localRot = Vector2.SignedAngle(facing, orbitAngleVector);

                    bool b1 = NormalizeAngle(localRot) >= minAng;
                    bool b2 = NormalizeAngle(localRot) <= maxAng;

                    SetOutputs(b1 && b2);

                    // Log angle and range when rocket is in the angle range and log interval has elapsed
                    if (b1 && b2 && Time.time - lastLogTime >= logInterval)
                    {
                        Debug.Log("Current angle: " + globalAngle);
                        Debug.Log("Rocket is in the angle range: [" + minAng + ", " + maxAng + "]");
                        lastLogTime = Time.time;
                    }
                }
                else
                {
                    float currentAngle = NormalizeAngle(rkt.rb2d.rotation);
                    bool b3 = currentAngle >= minAng;
                    bool b4 = currentAngle <= maxAng;

                    SetOutputs(b3 && b4);

                    // Log angle and range when rocket is in the angle range and log interval has elapsed
                    if (b3 && b4 && Time.time - lastLogTime >= logInterval)
                    {
                        Debug.Log("Current angle: " + currentAngle);
                        Debug.Log("Rocket is in the angle range: [" + minAng + ", " + maxAng + "]");
                        lastLogTime = Time.time;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in AngleSensorModule: " + e.Message);
            }
        }

        float NormalizeAngle(float a)
        {
            while (a < 0) { a += 360; }
            while (a >= 360) { a -= 360; }
            return a - 180;
        }
    }
}
