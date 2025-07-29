using System;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Xml.Schema;

namespace ImprovedBetterPowerScript {


public sealed class Program : MyGridProgram {


    #region ImprovedBetterPowerScript

    /*
    MIT License

    Copyright (c) 2025 Blayy

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    **THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.**
    */

//---------------------------------------------------------------------------------
    const string outputLcdName = "MB PowerDisplay";
    bool gridHasPowerProduction = true;
    bool gridHasPowerStorage = true;
//---------------------------------------------------------------------------------
    
    //Objects
    List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
    List<IMyBatteryBlock> powerStorage = new List<IMyBatteryBlock>();
    List<IMyTerminalBlock> gridBlocks = new List<IMyTerminalBlock>();
    List<string> subGridNames = new List<string>();
    IMyTextSurface terminalLCD;
    IMyTextPanel displayLCD;

    //Variables
    bool runScript = true;
    bool producerExists = false;
    bool storageExists = false;
    bool consumersExists = false;
    string debugOutput;

    //Methodes
    void writeOutput(string text ,bool clear) 
    {
        terminalLCD.WriteText(text, clear);
        Echo(text);
    }

    public Program() // Constructor
    {       
        // Runs every 1.6s
        Runtime.UpdateFrequency = UpdateFrequency.Update100;

        // Programmable Block (Defualt Output)
        terminalLCD = Me.GetSurface(0);
        writeOutput("Initializing Power Grid Script...\n\n",false); 

        // Try get needed objects
        displayLCD = GridTerminalSystem.GetBlockWithName(outputLcdName) as IMyTextPanel;

        /*
        Adds all blocks in a list in their own construct/grid
        */

        GridTerminalSystem.GetBlocks(gridBlocks);
        foreach (IMyTerminalBlock block in gridBlocks)
        {
            if (block.IsSameConstructAs(Me)) // Checks if block is part of the construct
            {

                if (block is IMyPowerProducer) // Checks for type Producer
                {
                    
                    if (block is IMyBatteryBlock) // Checks for type Battery
                    {
                        powerStorage.Add(block as IMyBatteryBlock);
                        writeOutput($"Found Battery: '{block.CustomName}'\n",true);
                    }
                    else
                    {
                        powerProducers.Add(block as IMyPowerProducer);
                        writeOutput($"Found Producer: '{block.CustomName}'\n",true);
                    }
                }    
            }
            else
            {

            }
        }

            /*
                Setting checks for debugging:
                    Has a working output LCD?
                    *Has al least one output is selected?
                    *Any Power Producers?
                    *Any Power Storage?
                    *Any Power Consumers?
                     
            */

            // Checks if there is a working displayLCD
            if (displayLCD != null)
            {
                if (displayLCD.IsWorking) 
                {
                    writeOutput($"Found LCD: '{outputLcdName}'\n",true);
                }
                else 
                { 
                    writeOutput($"{outputLcdName} not working\n",true);
                    writeOutput("Reverting to defualt LCD\n", true);
                }
            }
            else 
            {
                writeOutput($"LCD: '{outputLcdName}' not found\n",true);
            }

            // Checks for Power Producers.
            if (powerProducers.Count != 0) 
            {
                producerExists = true;
                writeOutput("Listed Producers succesfully\n", true);
            }
            else
            {
                writeOutput("No Producers found\n", true);
            }

            // Checks for Power Storage. 
            if (powerStorage.Count != 0)
            { 
                storageExists = true;
                writeOutput("Listed Storage succesfully\n", true);
            }
            else
            {
                writeOutput("No Storage found\n", true);
            }

            if (!producerExists && !storageExists && !consumersExists) 
            {
                runScript = false;
            }

            // Checks if user is dumb.
            if (!gridHasPowerProduction && !gridHasPowerStorage) 
            {
                runScript = false;
            }

            // Script will not run
            if (!runScript) 
            {
                writeOutput("Initialization failed...\n",true);
                writeOutput(debugOutput, false);
                return;
            }
            else
            {
                writeOutput("Script successfully Initialized", false);
            }

        }
    
    public void Save() // Save state
    { 
      if (!runScript) // Won't run the script if the requirments are not met.
        {
            return;
        }      
    }

    public void Main(string argument, UpdateType updateSource) // Running code
    {
        if (!runScript) // Won't run the script if the requirments are not met.
        {
            return;
        }
        
        string output = "Better Power Grid for Space Engineers:\n" +
                        "Updates every 1.6s\n\n";
        debugOutput = "Debug Information (Updates every 1.6s) :\n\n";
        
        
        string producersOutput = "";
        string storageOutput = "";

        debugOutput = "Debug Information (Updates every 1.6s):\n\n";

        if (gridHasPowerProduction)
        {
            // Producer's outputs
            float currentPowerProduced = 0f;
            float maxProducersOutput = 0f;

            // Loops through all the producers in the list
            foreach (IMyPowerProducer producer in powerProducers)
            {
                if (producer.IsWorking) // Checks if the producer is working
                {
                    currentPowerProduced += producer.CurrentOutput;
                    maxProducersOutput += producer.MaxOutput;
                }
                else // if not working add to debug
                {
                    debugOutput += $"'{producer.CustomName}' is not working.\n";
                }
            }
        
            double productionEfficiency = 0d;
            string powerProduced = "0 MWh";
            if (maxProducersOutput != 0) // Can't devide with 0.
            {
                // current/max * 100 gives %
                productionEfficiency = Math.Round(currentPowerProduced/maxProducersOutput*100,2);

                if (currentPowerProduced < 1) // less than one MW then to KW
                {
                    currentPowerProduced *= 1000;
                    powerProduced = $"{currentPowerProduced:F2} kWh"; 
                }
                else // else stay MW
                {
                    powerProduced = $"{currentPowerProduced:F2} MWh";
                }
            }

            // le output of current producer's information
            producersOutput =  "Power Production:\n"+
                                $"Power produced: {powerProduced}\n" + 
                                $"Max production: {maxProducersOutput:F2} MWh\n"+
                                $"Production Efficiency: {productionEfficiency}%";
        }
        
        if (gridHasPowerStorage) // Grid has power Storage flag.
        {
            // Storage's outputs
            float currentPowerStored = 0f;
            float maxPowerStorage = 0f;
            float currentStorageOutput = 0f;

            foreach (IMyBatteryBlock storage in powerStorage)
            {
                if (storage.IsWorking) // Checks if the battery is working
                {
                    currentPowerStored += storage.CurrentStoredPower;
                    maxPowerStorage += storage.MaxStoredPower;
                    currentStorageOutput += storage.CurrentOutput; 

                }
                else // if not working add to debug
                {
                    debugOutput += $"'{storage.CustomName}' is not working.\n";
                }
            }

            double storageEfficiency = 0d;
            float timeLeft = 0f;
            string powerLeft = $"{timeLeft:F2} s";
            if (maxPowerStorage != 0)
            {
                if (currentPowerStored <= 1) // Les or = to 1 MW then convert kW back to MW
                {

                }
                
                storageEfficiency = Math.Round(currentPowerStored/maxPowerStorage*100,2);
                
                timeLeft = currentPowerStored/currentStorageOutput ; // Gets in hours 
                if(timeLeft <= 1 )
                {
                    timeLeft *= 60; // to mins
                    powerLeft = $"{timeLeft:F2} mins";
                
                    if (timeLeft <= 60)
                    {
                        timeLeft *= 60; // To seconds
                        powerLeft = $"{timeLeft:F2} s";
                    }
                }
                else
                {   
                    if (timeLeft != float.PositiveInfinity)
                    {
                        powerLeft = $"{timeLeft:F2} hours";
                    }
                    else
                    {
                        powerLeft = "Infinity";
                    } 
                }   
            }
            // Power storage output
            storageOutput = "Power Storage:\n" +
                            $"Power Stored: {currentPowerStored:F2} MW\n" +
                            $"Max Storage: {maxPowerStorage:F2} MW\n" +
                            $"Storage at: {storageEfficiency}%\n" +
                            $"Time before depleted: {powerLeft}";
        }
        
        // What the output should be depending on the user's needs. 
        if(gridHasPowerProduction && gridHasPowerStorage) // Show both
        {
            output += producersOutput +"\n\n"+ storageOutput;
        }
        else if (!gridHasPowerProduction) // Only power Storage
        {
            output += storageOutput;
        }
        else if (!gridHasPowerStorage) // Only Power production
        {
            output += producersOutput;
        }
        
        output += "\n\nDebug information sent to console.";
        
      if (displayLCD != null)
            {
                if (displayLCD.IsWorking) 
                {
                    displayLCD.WriteText(output, false);
                    terminalLCD.WriteText(debugOutput);
                }
                else 
                {
                    debugOutput += $"LCD: '{outputLcdName}' not working.\n";
                    terminalLCD.WriteText(output, false);
                    Echo(debugOutput);
                }
            }
            else 
            {
                terminalLCD.WriteText(output);
                debugOutput += $"LCD: '{outputLcdName}' not found.\n";
                terminalLCD.WriteText(output, false);
                Echo(debugOutput);
            }   
    }

    #endregion // ImprovedBetterPowerScript
}}
