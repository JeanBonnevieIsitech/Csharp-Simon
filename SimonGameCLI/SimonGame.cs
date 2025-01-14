﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using DiscordRPC;

namespace SimonGameCLI
{
    internal class SimonGame
    {
        private Random random = new Random();
        private int score;
        private int delay;
        private string savePath;
        private string saveFilename;
        private string saveFilenamePath;

        private char[] arrowArray;
        private Dictionary<char, ConsoleKey> ArrowDict;

        //  List of char to memorize for each round
        private List<char> levelCharList = new List<char>();

        private string errorCreateSave;
        private string errorWriteSave;
        private string errorReadSave;

        // discord rich presence
        discordRPC discord;


        // constructeur
        public SimonGame(discordRPC discord)
        {
            // Games variable intialisation
            score = 0;
            delay = 2000;
            arrowArray = new char[] { '→', '←', '↑', '↓' };

            //  Makes the correspondence between the characters of arrows and the name of the key
            ArrowDict = new Dictionary<char, ConsoleKey>();
            ArrowDict.Add('→', ConsoleKey.RightArrow);
            ArrowDict.Add('←', ConsoleKey.LeftArrow);
            ArrowDict.Add('↑', ConsoleKey.UpArrow);
            ArrowDict.Add('↓', ConsoleKey.DownArrow);

            // Other variables

            // folder where save file is located
            savePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SimonGame";
            // save file name
            saveFilename = "save.json";
            // concatenation of savePath and fileName to get a complete path
            saveFilenamePath = $"{savePath}\\{saveFilename}";
            // Error text
            errorCreateSave = "Erreur lors de la création du fichier de sauvegarde";
            errorWriteSave = "Erreur lors de l'écriture du fichier de sauvegarde";
            errorReadSave = "Erreur lors de la lecture du fichier de sauvegarde";

            this.discord = discord;
        }

        // FUNCTIONS
        void message(string msg)
        {
            Console.WriteLine(msg + "\nAppuyez sur une touche pour continuer...");
            Console.ReadKey();
            Console.Clear();
        }

        void afficherLevel()
        {
            Console.Clear();
            for (int i = 0; i < levelCharList.Count; i++)
            {
                Console.Write(" " + levelCharList[i]);
                if (i != levelCharList.Count - 1)
                {
                    Thread.Sleep(delay / 2);
                }

            }
            Thread.Sleep(delay);
            Console.Clear();
        }

        void initSaveFile()
        {
            try
            {
                Directory.CreateDirectory(savePath);
                if (!File.Exists(saveFilenamePath))
                {
                    Dictionary<string, int> scoreDict = new Dictionary<string, int>();
                    scoreDict["highscore"] = -1;
                    File.Create(saveFilenamePath).Close();
                    File.WriteAllText(saveFilenamePath, JsonConvert.SerializeObject(scoreDict));
                }
            }

            catch (Exception e)
            {
                message($"{errorCreateSave} :\n{e.Message}");
                //Environment.Exit(1);
            }
        }

        void saveScoreTofile(int score)
        {
            try
            {
                var scoreDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(saveFilenamePath));
                if (score > scoreDict["highscore"] && score > 0)
                {
                    scoreDict["highscore"] = score;
                    message("Et c'est un nouveau record !");

                }

                File.WriteAllText(saveFilenamePath, JsonConvert.SerializeObject(scoreDict));

            }

            catch (Exception e)
            {
                message($"{errorWriteSave} :\n{e.Message}");
                //Environment.Exit(1);
            }
        }

        int getHighscoreFromFile()
        {
            try
            {

                var scoreDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(saveFilenamePath));
                return scoreDict["highscore"];
            }

            catch (Exception e)
            {
                message($"{errorReadSave}\n{e.Message}");
                //sEnvironment.Exit(1);
            }
            return 0;
        }



        public void run()
        {
            // MAIN
            Console.Clear();
            initSaveFile();

            discord.gameState.State = "En train de jouer";
            discord.gameState.Timestamps = Timestamps.Now;

            if (getHighscoreFromFile() > 0) { discord.gameState.Details = $"\nMeilleur score : {getHighscoreFromFile()}"; }

            message("Bienvenue dans le simon console !");
            message("Mémorisez l'ordre des flèches qui vont s'afficher");

            if (getHighscoreFromFile() > 0)
            {
                message($"Le meilleur score acutellement enregistré est de {getHighscoreFromFile()}");
            }

            discord.update();
            bool running = true;
            // MAIN LOOP

            ConsoleKey key;
            while (running)
            {
                levelCharList.Add(arrowArray[random.Next(arrowArray.Length)]);
                afficherLevel();

                // prevent from cheating by pressing key before readkey
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }

                Console.WriteLine("C'est à vous :");
                for (int n = 0; n < levelCharList.Count; n++)
                {
                    key = Console.ReadKey().Key;
                    if (ArrowDict[levelCharList[n]] != key)
                    {
                        Console.Clear();
                        //Environment.Exit(0);
                        running = false;
                        discord.gameState.Timestamps = null;
                        discord.gameState.State = "Vient de perdre";
                        discord.update();
                        message($"Perdu !\nVotre score est de : {score}");
                        saveScoreTofile(score);
                        discord.gameState.State = "Dans les menus";
                        discord.update();
                        break;
                    }

                    if (running) { Console.Write(" " + levelCharList[n]); }

                }

                if (running)
                {
                    score++;
                    discord.gameState.State = $"Score actuel : {this.score}";
                    discord.update();
                    Thread.Sleep(delay / 2);
                }

            }

        }
    }
}
