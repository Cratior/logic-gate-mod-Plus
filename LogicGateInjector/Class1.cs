using UnityEngine;
using System;
using System.Collections.Generic;
using SFS.Parts.Modules;
using SFS.Variables;
using Sirenix.OdinInspector;
using SFS.Input;
using SFS.UI;
using static System.Net.Mime.MediaTypeNames;
using SFS.World;
using SFS.World.Maps;
using SFS;
using SFS.World;
using SFS.WorldBase;
using SFS.Input;
namespace LogicGateInjector
{
    public class LogicGateModule : MonoBehaviour
    {
        // Input triggers for the gate
        public InputTrigger[] inputs;

        // Output game objects
        public GameObject[] outputs;

        // Type of gate
        public GateType gate;

        // State of toggle gate
        private bool toggleState = false;
        private bool lastInputState = false;

        [BoxGroup("Tex")]
        public String_Reference KeyBind;


        // Set all outputs to a given value
        public void SetAllOutputs(bool value, float mult = 1f)
        {
            foreach (GameObject go in outputs)
            {
                if (go)
                    go.transform.localPosition = new Vector3(go.transform.localPosition.x, value ? 0f : (0.1f * mult), go.transform.localPosition.z);
            }
        }
        // Update the gate logic
        public void FixedUpdate()
        {
            switch (gate)
            {
                case GateType.Wire:
                    bool found = false;
                    foreach (var input in inputs)
                    {
                        if (input)
                        {
                            if (input.enab)
                            {
                                found = true;
                            }
                        }
                    }
                    SetAllOutputs(found);
                    break;

                case GateType.AND:
                    SetAllOutputs(inputs[0].enab && inputs[1].enab);
                    break;

                case GateType.OR:
                    SetAllOutputs(inputs[0].enab || inputs[1].enab);
                    break;

                case GateType.NOT:
                    SetAllOutputs(!inputs[0].enab);
                    break;

                case GateType.XOR:
                    SetAllOutputs(inputs[0].enab ^ inputs[1].enab);
                    break;

                case GateType.NOR:
                    SetAllOutputs(!(inputs[0].enab || inputs[1].enab)); // NOR logic: !(A OR B)
                    break;

                case GateType.NAND:
                    SetAllOutputs(!(inputs[0].enab && inputs[1].enab)); // NAND logic: !(A AND B)
                    break;
                case GateType.XNOR:
                    SetAllOutputs(!(inputs[0].enab ^ inputs[1].enab)); // XNOR logic: !(A XOR B)
                    break;
                case GateType.Toggle:
                    // Check if input has changed
                    bool currentInputState = inputs[0].enab;
                    if (currentInputState && !lastInputState) // If input has changed from inactive to active
                    {
                        toggleState = !toggleState; // Toggle the state
                    }
                    lastInputState = currentInputState; // Update last input state

                    // Set output to the current state
                    SetAllOutputs(toggleState);
                    break;

                case GateType.MagnetOutput:
                    bool foundInp = false;
                    foreach (var input in inputs)
                    {
                        if (input)
                        {
                            if (input.enab)
                            {
                                foundInp = true;
                            }
                        }
                    }
                    SetAllOutputs(foundInp, 20f);
                    break;
                case GateType.SevenSegmentDisplay:
                    // Logic for Seven Segment Display gate
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        bool isInputEnabled = inputs[i].enab;

                        // Toggle the active state of the corresponding output objects for the segment
                        if (isInputEnabled)
                        {
                            outputs[i * 2].SetActive(true); // Enabled state
                            outputs[i * 2 + 1].SetActive(false); // Disabled state
                        }
                        else
                        {
                            outputs[i * 2].SetActive(false); // Enabled state
                            outputs[i * 2 + 1].SetActive(true); // Disabled state
                        }
                    }
                    break;
                case GateType.KeyPress:
                    // Check if the specified key is pressed
                    bool keyPress = Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), KeyBind.Value));
                    SetAllOutputs(keyPress);
                    break;

                default:
                    break;
            }
        }
    }

    // Enumeration for gate types
    public enum GateType
    {
        AND,
        NOT,
        OR,
        XOR,
        XNOR,
        NOR,
        NAND,
        Wire,
        Output,
        MagnetOutput,
        Toggle,
        SevenSegmentDisplay,
        KeyPress
    }

    public class ExternalModule : MonoBehaviour
    {
        // Type of the module
        public int moduleType;

        // List of variables
        public List<UnityEngine.Object> vars = new List<UnityEngine.Object>();
    }

    // Class for output module
    public class OutputModule : MonoBehaviour
    {
        // Game objects for enabled and disabled models
        public GameObject modelEnabled;
        public GameObject modelDisabled;

        // Input trigger
        public InputTrigger input;

        // Update method to toggle models
        public void Update()
        {
            if (input.enab)
            {
                modelEnabled.SetActive(true);
                modelDisabled.SetActive(false);
            }
            else
            {
                modelEnabled.SetActive(false);
                modelDisabled.SetActive(true);
            }
        }
    }
    public class ToggleModule : MonoBehaviour
    {
        public GameObject modelEnabled;
        public GameObject modelDisabled;

        public Transform outputTrigger;

        // toggle models
        public void Update()
        {
            float triggerPositionY = outputTrigger.localPosition.y;

            if (Mathf.Approximately(triggerPositionY, 0f))
            {
                modelEnabled.SetActive(true);
                modelDisabled.SetActive(false);
            }
            else if (Mathf.Approximately(triggerPositionY, 0.1f))
            {
                modelEnabled.SetActive(false);
                modelDisabled.SetActive(true);
            }
        }
    }

    // Input trigger class
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class InputTrigger : MonoBehaviour
    {
        private Rigidbody2D rb2d;
        private int layer;
        private CircleCollider2D trigger;

        [HideInInspector] public bool enab;

        public int inputID;

        public LogicGateModule lgm;

        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            rb2d.isKinematic = true;
            trigger = GetComponent<CircleCollider2D>();
            layer = LayerMask.NameToLayer("Docking Trigger");

            if (lgm)
                lgm.inputs[inputID] = this;
        }

        public void FixedUpdate()
        {
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (lgm)
            {
                if (lgm.gate == GateType.MagnetOutput && other.GetComponentInChildren<DockingPortTrigger>() != null)
                {
                    return;
                } // Stop the magnets from interfering with the DP converter input 
            }
            enab = true;
        }

        // Trigger exit method
        public void OnTriggerExit2D(Collider2D other)
        {
            if (lgm)
            {
                if (lgm.gate == GateType.MagnetOutput && other.GetComponentInChildren<DockingPortTrigger>() != null)
                {
                    return;
                } // Stop the magnets from interfering with the DP converter input 
            }
            enab = false;
        }
    }
}