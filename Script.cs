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
using VRage.Game.ObjectBuilders.VisualScripting;
using System.Reflection;
using System.Threading;
using VRage.Network;

namespace BetterPowerScript {

public sealed class Program : MyGridProgram {

    #region BetterPowerScript

    /*
    Constants for names of Block Groups and Display LCD.
    The text between the quotes("") should match the names of the groups and display LCD on your grid.
    You can change them to match your setup. 
    */ 

    const string mainGridName = "MB PowerGrid"; /* Change the text between quotes("") to the NAME grid that the script runs on. */
    const string producerGroup = "MB PowerProducer"; /* Change the text between quotes("") to the NAME of the group that contains your Power Producers. */
    const string storageGroup = "MB PowerStorage"; /* Change the text between quotes("") to the NAME of the group that contains your Power Storage  on. */
    const string outputDisplay = "MB PowerDisplay"; /* Change the text between quotes("") to the name of the LCD you want the output to be displayed on. */
    
    /*
    Change gridHasPowerProducer to false if your grid does not have any power producers or you don't want "Power Producer information" to be displayed.
    Change gridHasPowerStorage to false if your grid does not have any battery blocks or you don't want "Power Storage information" to be displayed. 
    Change gridHasPowerConsumer to false if your grid does not have any power consumers or you don't want "Power Consumption information" to be displayed.
    */ 

    bool gridHasPowerProducer = true;   /* Want set, true
                                            else, false */     
    bool gridHasPowerStorage = true;    /* Want set, true
                                            else, false */ 
    bool gridHasPowerConsumer = true;   /* Want, true
                                            else, false */

//----------------------------------------------------------------------------------------------------------------------------------------
    // Don't change the following variables and code or the script will not work.

    // Objects
    IMyTextSurface terminalLCD;
    IMyTextPanel displayLCD;
    List<IMyBlockGroup> gridGroups = new List<IMyBlockGroup>();
    List <IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
    List <IMyBatteryBlock> powerStrorage = new List<IMyBatteryBlock>();
    
    // Variables
    bool bRunScript = true;
    bool producerExist;
    bool storageExist;
    bool displayLCDExist;
    string output;
    string debugOutput = string.Empty;

    // Methodes
    void writeOutput(string textToWrite, bool keep) // Writing to the terminal display.
    {
        terminalLCD.WriteText(textToWrite);

        if (!keep) 
        { 
            Echo(debugOutput);
        }
            
    }

//---------------------------------------------------------------------------------------------------------------------------------------
    // RunScript method to control the script execution.
    // If bRun is false, it will not run the script and will output a message
    public Program() // Constructor 
    {
        // Set the update frequency to update every 100 ticks (1.67 seconds).
        Runtime.UpdateFrequency = UpdateFrequency.Update100;

        // Initialize the script and output a message to the terminal.
        terminalLCD = Me.GetSurface(0);
        terminalLCD.ContentType = ContentType.TEXT_AND_IMAGE;
        terminalLCD.FontSize = 0.8f;
        writeOutput("Initializing Power Grid Script...\n\n",false);

        // Initialize the display LCD.
        displayLCD = GridTerminalSystem.GetBlockWithName(outputDisplay) as IMyTextPanel;
        if (displayLCD == null)
        {
            writeOutput($"LCD '{outputDisplay}' not found. Please ensure the LCD is named '{outputDisplay}'.",true);
            writeOutput("Output will default to the Programmable Block's LCD.",true);
            writeOutput("Debug information will be sent to the console.",true);

            displayLCDExist = false;
        }
        else 
        {   
            if (displayLCD.IsWorking == false)
            {
                writeOutput($"Display LCD '{outputDisplay}' is not working.", true);
                writeOutput("Output will default to the Programmable Block's LCD.", true);
                writeOutput("Debug information will be sent to the console.", true);
            }
            else
            {
                writeOutput($"Display LCD '{outputDisplay}' found and is working.",true);
                displayLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                displayLCD.FontSize = 1f;
                displayLCD.WriteText("Initializing Power Grid Script...");
            }
                displayLCDExist = true;
        }

        // Check if the groups exist in the grid.
        producerExist = GridTerminalSystem.GetBlockGroupWithName(producerGroup) != null;
        storageExist = GridTerminalSystem.GetBlockGroupWithName(storageGroup) != null;        

        // If neither group exists, output a message and stop the script.
        if (!producerExist && !storageExist) 
        {
            writeOutput($"No groups found with the names '{producerGroup}' or '{storageGroup}'.",true);
            bRunScript = false;
            return;
        }

        powerProducers.Clear();   
        powerStrorage.Clear(); 

        // Get all the groups on the grid. (NEEDS TO BE REWORKED)
        GridTerminalSystem.GetBlockGroups(gridGroups); // ONLY GET MAIN GRID INFO, EVEN IF SUBGRIDS ARE CONNECTED!

        List<IMyPowerProducer> NoBatteries = new List<IMyPowerProducer>();

        foreach (IMyBlockGroup group in gridGroups)
        {
            if (group.Name == producerGroup)
            {
                // Get all power producers in the group.
                group.GetBlocksOfType<IMyPowerProducer>(powerProducers);
                foreach (IMyPowerProducer producer in powerProducers)
                {   
                    // Check if the producer is not a battery block.
                    // If it is not, add it to the NoBatteries list.
                    var batteryBlock = producer as IMyBatteryBlock;
                    if (batteryBlock == null)
                    {
                        NoBatteries.Add(producer);
                        writeOutput($"Found power producer: {producer.CustomName}",true);
                    }
                }
                powerProducers.Clear();
                powerProducers.AddRange(NoBatteries);
                
                // Clear memory of NoBatteries to avoid duplicates in the next iteration.
                NoBatteries.Clear();
            }
            else if (group.Name == storageGroup)
            {
                // Get all battery blocks in the group.
                group.GetBlocksOfType<IMyBatteryBlock>(powerStrorage);
                foreach (IMyBatteryBlock battery in powerStrorage)
                {
                    writeOutput($"Found battery: {battery.CustomName}",true);
                }
            }
        }

        // Check if any power producers or battery blocks were found.
        if (powerProducers.Count == 0 && powerStrorage.Count == 0)
        {
            // If no power producers or battery blocks were found, output a message and stop the script.
            bRunScript = false;  
            return;
        }
        else 
        { 
            if ( gridHasPowerProducer || gridHasPowerStorage)
            {
                // Output the number of power producers and battery blocks found.
                writeOutput($"Found {powerProducers.Count} power producers.",true);
                writeOutput($"Found {powerStrorage.Count} battery blocks.",true);
                writeOutput("Power Grid Script Initialized",true);
            }
                else // If gridhaspowerprocuder and gridhaspowerstorage are false.
                {
                    writeOutput("Both 'gridHasPowerProducer' and 'gridHasPowerStorage' can't be false.\nScript will not run.",true);
                    bRunScript = false;
                    return;
                }
            }

    }

    public void Save() 
    {
        // This method is called when the script is saved.
        // You can use it to save any state or data that you want to keep between script runs.
        // In this case, we are not saving any state, so we leave it empty.
        // If you want to save data, you can use the Storage property of the Program class.
    }
    public void Main(string argument, UpdateType updateSource)
    {
        // Check if the script is running and output a message if it is not.
        if (!bRunScript)
        {
            return;
        }

        // Clear the output strings before each run. 
        output = "Power Grid Status (Updates every 1.6s):\n\n";
        debugOutput = "Debug Information (Updates every 1.6s):\n\n";

        string producerOutput = string.Empty;
        string storageOutput = string.Empty;

        if (gridHasPowerProducer) // If user want to have a PowerProducer output
        {
            // Initialize variables to store total power produced and maximum power possible.
            float totalCurrentPowerProduced = 0f;
            float totalMaxPowerPossible = 0f;
            double totalProductionEfficiency = 0d;

            // Checks if the PowerProduction group Exists on grid
            if (!producerExist)
            {
                debugOutput += $"Production Group '{producerGroup}' not found.\n";
            }
            else
            {   
                // Get the total current power produced and maximum power possible from all power producers.
                foreach (IMyPowerProducer producer in powerProducers)
                {
                    if ( producer.IsWorking) // Checks if selected producer is working or not. 
                    { 
                        totalCurrentPowerProduced += producer.CurrentOutput;
                        totalMaxPowerPossible += producer.MaxOutput;
                    }
                    else
                    {
                        debugOutput += $"Producer '{producer.CustomName}' not working.\n";
                    }
                }
                    
                // Can't devide by zero, so check if totalMaxPowerPossible is not zero before calculating the percentage.
                if (totalMaxPowerPossible != 0f)
                {
                    totalProductionEfficiency = Math.Round(totalCurrentPowerProduced / totalMaxPowerPossible * 100, 2);
                }     
            }

            // Output the total current power produced, maximum power possible, and production efficiency.
            producerOutput =   "Power Production:\n" +
                                $"Total Current Power Produced: {totalCurrentPowerProduced:F2} MW\n" +
                                $"Total Max Power Possible: {totalMaxPowerPossible:F2} MW\n" +
                                $"Production Efficiency: {totalProductionEfficiency}%";
            }

            if (gridHasPowerStorage) // If user want to have a PowerStorage output
            { 
                // Initialize variables to store total current power stored and maximum power stored.
                float totalCurrentPowerStored = 0f;
                float totalMaxPowerStorage = 0f;
                double totaltStorageUsed = 0d;

                if (!storageExist) 
                {   
                    // Adds to the debugOutput if the StorageGroup does not exist. 
                    debugOutput += $"Storage Group '{storageGroup}' not found.\n";
                }
                else 
                {

                    // Get the total current power stored and maximum power stored in all battery blocks.
                    foreach (IMyBatteryBlock battery in powerStrorage)
                    {
                        if (battery.IsWorking) // Checks if selected battery is working or not. 
                        { 
                            totalCurrentPowerStored += battery.CurrentStoredPower;
                            totalMaxPowerStorage += battery.MaxStoredPower;
                        }
                        else
                        {
                            debugOutput += $"Battery '{battery.CustomName}' not working.\n";
                        }
                    }

                    // Can't devide by zero, so check if totalMaxPowerStorage is not zero before calculating the percentage.
                    if (totalMaxPowerStorage != 0f )
                    {   
                        totaltStorageUsed = Math.Round(totalCurrentPowerStored / totalMaxPowerStorage * 100, 3);
                    }
                }

                // Output the total current power stored, maximum power storage, and storage used percentage.
                storageOutput =     "Power Storage:\n" +
                                    $"Total Current Power Stored: {totalCurrentPowerStored:F2} MW\n" +
                                    $"Total Possible Power Storage: {totalMaxPowerStorage:F2} MW\n" +
                                    $"Power Storage at: {totaltStorageUsed}%";
            }

            if (gridHasPowerProducer && gridHasPowerStorage) 
            {
                // If both power producers and battery blocks exist, output the combined status.
                output += producerOutput + "\n\n" + storageOutput;
            }
            else if (gridHasPowerProducer) 
            {
                // If only power producers exist, output the power producers status.
                output += producerOutput;
            }
            else if (gridHasPowerStorage) 
            {
                // If only battery blocks exist, output the battery blocks status.
                output += storageOutput;
            }

            output += "\n\n Debug Information send to the console.";

            // Output the results to the display LCD or terminal.
            if (displayLCDExist) 
            {
                if (displayLCD.IsWorking)  
                {
                    displayLCD.WriteText(output);
                    writeOutput(debugOutput,false);
                }
                else
                {   
                    debugOutput += $"Display LCD '{outputDisplay}' is not working.\n" +
                                    "Output will default to the Programmable Block's LCD.\n";
                    writeOutput(output,false);
                }    
            }
            else 
            {   
                debugOutput += $"Display LCD '{outputDisplay}' does not exist.\n" +
                                "Output will default to the Programmable Block's LCD.\n";
                writeOutput(output,false);
            }   

        }
        #endregion // BetterPowerScript
    } 

}   