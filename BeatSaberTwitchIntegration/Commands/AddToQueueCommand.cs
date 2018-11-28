﻿using System.Text.RegularExpressions;
using AsyncTwitch;
using JetBrains.Annotations;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    [UsedImplicitly]
    public class AddToQueueCommand : IrcCommand
    {
        public override string[] CommandAlias => new [] {"bsr", "add", "kloudr", "gib", "gibsong", "tomato", "awoo"};
        private readonly Regex _songIdrx = new Regex(@"^[0-9\-]+$", RegexOptions.Compiled);
        
        public override void Run(TwitchMessage msg)
        {
            if (!StaticData.TwitchMode && !msg.Author.IsMod && !msg.Author.IsBroadcaster)
            {
                TwitchConnection.Instance.SendChatMessage("The Queue is currently closed.");
                return;
            }

            string queryString = msg.Content.Remove(0, msg.Content.IndexOf(' ') + 1);
            bool isTextSearch = !_songIdrx.IsMatch(queryString);
            
            QueuedSong request = ApiConnection.GetSongFromBeatSaver(isTextSearch, queryString,  msg.Author.DisplayName);
            if (request.SongHash == "" || request.Id == "")
            {
                if (isTextSearch)
                {
                    request = ApiConnection.GetSongFromBeatSaver(true, "\"" + queryString + "\"",
                        msg.Author.DisplayName);
                    if (request.SongHash == "" || request.Id == "")
                    {
                        TwitchConnection.Instance.SendChatMessage("Invalid Request.");
                        return;
                    }
                }
                else
                {
                    TwitchConnection.Instance.SendChatMessage("Invalid Request.");
                    return;
                }
            }
            if (StaticData.BanList.IsBanned(request.Id))
            {
                TwitchConnection.Instance.SendChatMessage("Song is currently banned.");
                return;
            }

            if (msg.Author.IsMod || msg.Author.IsBroadcaster)
            {
                AddToQueue(request);
                return;
            }

            if (StaticData.UserRequestCount.ContainsKey(msg.Author.DisplayName))
            {
                int requestLimit = msg.Author.IsSubscriber
                    ? StaticData.Config.SubLimit
                    : StaticData.Config.ViewerLimit;
                if (StaticData.UserRequestCount[msg.Author.DisplayName] >= requestLimit)
                {
                    TwitchConnection.Instance.SendChatMessage(
                        msg.Author.DisplayName + " you're making too many requests. Slow down.");
                    return;
                }

                if (AddToQueue(request))
                    StaticData.UserRequestCount[msg.Author.DisplayName]++;
            }
            else
            {
                if(AddToQueue(request))
                    StaticData.UserRequestCount.Add(msg.Author.DisplayName, 1);
            }

        }

        private bool AddToQueue(QueuedSong song)
        {
            if (StaticData.SongQueue.IsSongInQueue(song))
            {
                TwitchConnection.Instance.SendChatMessage("Song already in queue.");
                return false;
            }

            StaticData.SongQueue.AddSongToQueue(song);
            TwitchConnection.Instance.SendChatMessage($"{song.RequestedBy} added \"{song.SongName}\", uploaded by: {song.AuthName} to queue!");
            return true;
        }
    }
}