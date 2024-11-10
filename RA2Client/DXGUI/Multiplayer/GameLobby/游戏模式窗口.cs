﻿using ClientGUI;
using Microsoft.Xna.Framework;
using Ra2Client.Domain.Multiplayer;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    public class 添加至游戏模式(WindowManager windowManager,Map map, GameModeMapCollection gameModeMaps) : XNAWindow(windowManager)
    {
        private XNADropDown 游戏模式选项框;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0,0,275,100);

            Name = "添加至游戏模式";

           

            var lblMapName = new XNALabel(windowManager)
            {
                Text = $"地图名: {map.Name}",
                ClientRectangle = new Rectangle(10, 10, 0, 0),
            };

            var 添加至 = new XNALabel(windowManager)
            {
                Text = "添加至: ",
                ClientRectangle = new Rectangle(lblMapName.X, lblMapName.Bottom + 30, 0, 0)
            };

            游戏模式选项框 = new XNADropDown(windowManager) { 
                ClientRectangle = new Rectangle(添加至.Right + 50, 添加至.Y, 120, 30)
            };

            gameModeMaps.GameModes.ForEach(gm => 游戏模式选项框.AddItem(gm.Name));

            var 新增模式 = new XNAButton(windowManager)
            {
                Text = "新增",
                ClientRectangle = new Rectangle(游戏模式选项框.Right + 10, 添加至.Y, 75, 23),
                IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
                HoverSoundEffect = new EnhancedSoundEffect("button.wav")
            };

            新增模式.LeftClick += (_,_) => { };

            var 确定 = new XNAButton(windowManager)
            {
                Text = "确定",
                ClientRectangle = new Rectangle(添加至.X, 游戏模式选项框.Bottom + 5, 75, 23),
                IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
                HoverSoundEffect = new EnhancedSoundEffect("button.wav")
            };

            确定.LeftClick += (_, _) => {
                if (map.GameModes.Contains(游戏模式选项框.SelectedItem.Text))
                {
                    XNAMessageBox.Show(WindowManager, "信息", "该地图已经在该游戏模式中了!");
                    return;
                }

                var gameModes = map.GameModes.ToList();
                
                map.GameModes = [.. gameModes, 游戏模式选项框.SelectedItem.Text];

                gameModeMaps.ForEach(gm => {
                    if (gm.GameMode.UIName == 游戏模式选项框.SelectedItem.Text)
                    {
                        gm.GameMode.Maps.Add(map); 
                    }
                    if(gm.Map.SHA1 == map.SHA1)
                        gm.Map.GameModes = [.. gm.Map.GameModes, 游戏模式选项框.SelectedItem.Text];
                        });

                //var maps = mapLoader.GameModes.Find(gm => gm.Name == 游戏模式选项框.SelectedItem.Text).Maps;

                // mapLoader.GameModes.Find(gm => gm.Name == 游戏模式选项框.SelectedItem.Text).Maps = [.. maps, map];

                XNAMessageBox.Show(WindowManager, "信息", "移动成功!");
                Disable();
                return;
            };


            var 取消 = new XNAButton(windowManager)
            {
                Text = "取消",
                ClientRectangle = new Rectangle(新增模式.X, 确定.Y, 75, 23),
                IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png"),
                HoverSoundEffect = new EnhancedSoundEffect("button.wav")
            };

            取消.LeftClick += (_, _) => {Disable(); };

            AddChild([lblMapName, 添加至, 游戏模式选项框, 新增模式, 确定, 取消]);

            CenterOnParent();
            base.Initialize();
        }
    }
}
