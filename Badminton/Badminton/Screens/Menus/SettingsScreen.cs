﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Badminton.Screens.MultiPlayer;
using Badminton.Screens.Menus.Components;

namespace Badminton.Screens.Menus
{
	class SettingsScreen : GameScreen
	{
		private PlayerSelect prevScreen;
		bool upPressed, downPressed, leftPressed, rightPressed, startPressed, enterPressed;

		float gravity;
		float limbStrength;
		int timeLimit;
		int lives;

		List<Component> components;
		List<Texture2D> maps;
		Dictionary<string, bool> checkValues;
		int selectedComponent, selectedMap;

		public SettingsScreen(PlayerSelect prevScreen)
		{
			this.prevScreen = prevScreen;
			upPressed = true;
			downPressed = true;
			leftPressed = true;
			rightPressed = true;
			startPressed = true;
			enterPressed = true;

			gravity = 9.8f;
			limbStrength = 1f;
			timeLimit = 0;
			lives = 5;

			selectedComponent = 0;
			selectedMap = 0;

			maps = new List<Texture2D>();
			maps.Add(MainGame.tex_bg_castle);

			checkValues = new Dictionary<string, bool>();
			components = new List<Component>();
			components.Add(new Button(new Vector2(1160, 235), MainGame.tex_btnUp, "map"));
			components.Add(new Button(new Vector2(1160, 315), MainGame.tex_btnUp, "grav"));
			components.Add(new Button(new Vector2(1160, 395), MainGame.tex_btnUp, "limb"));
			components.Add(new Button(new Vector2(1160, 475), MainGame.tex_btnUp, "time"));
			components.Add(new Button(new Vector2(1160, 555), MainGame.tex_btnUp, "lives"));
			components.Add(new CheckBox(new Vector2(800, 635), "Sudden death mode", "death"));
			checkValues.Add("death", false);
			components.Add(new CheckBox(new Vector2(800, 715), "Allow ranged attacks", "ranged"));
			checkValues.Add("ranged", false);
			components.Add(new CheckBox(new Vector2(800, 795), "Allow mines", "traps"));
			checkValues.Add("traps", false);
			components.Add(new CheckBox(new Vector2(800, 875), "Fill empty slots with bots", "bots"));
			checkValues.Add("bots", false);
			components.Add(new Button(new Vector2(960, 980), MainGame.tex_ps_next, "start"));
			components[0].Selected = true;

			((CheckBox)components[6]).Checked = true;
			checkValues["ranged"] = true;
			((CheckBox)components[7]).Checked = true;
			checkValues["traps"] = true;
			if (prevScreen.Colors.Length == 1)
			{
				checkValues["bots"] = true;
				((CheckBox)components[8]).Checked = true;
			}
		}

		public GameScreen Update(GameTime time)
		{
			Component.UpdateSelection(components);
			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.B))
				return GoBack();

			GameScreen ret = null;
			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.LeftThumbstickLeft) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadLeft) || Keyboard.GetState().IsKeyDown(Keys.Left))
				ret = UpdateSelection(false, ref leftPressed);
			else
				leftPressed = false;

			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.LeftThumbstickRight) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadRight) || Keyboard.GetState().IsKeyDown(Keys.Right))
				ret = UpdateSelection(true, ref rightPressed);
			else
				rightPressed = false;

			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.A) || Keyboard.GetState().IsKeyDown(Keys.Enter))
				ret = UpdateSelection(true, ref enterPressed);
			else
				enterPressed = false;

			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.LeftThumbstickUp) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadUp) || Keyboard.GetState().IsKeyDown(Keys.Up))
			{
				if (!upPressed)
				{
					upPressed = true;
					selectedComponent = selectedComponent == 0 ? components.Count - 1 : selectedComponent - 1;
					Component.GetSelectedComponent(components).Selected = false;
					components[selectedComponent].Selected = true;
				}
			}
			else
				upPressed = false;

			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.LeftThumbstickDown) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadDown) || Keyboard.GetState().IsKeyDown(Keys.Down))
			{
				if (!downPressed)
				{
					downPressed = true;
					selectedComponent = selectedComponent == components.Count - 1 ? 0 : selectedComponent + 1;
					Component.GetSelectedComponent(components).Selected = false;
					components[selectedComponent].Selected = true;
				}
			}
			else
				downPressed = false;

			if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Start))
			{
				if (!startPressed)
				{
					startPressed = true;
					switch (prevScreen.Mode)
					{
						// Pass parameters eventually
						case -1:
							return new FreeForAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 0:
							return new FreeForAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 1:
							return new TeamDeathmatch(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 2:
							return new OneVsAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
					}
				}
			}
			else
				startPressed = false;

			if (prevScreen.Colors.Length == 1)
			{
				((CheckBox)components[8]).Checked = true;
				checkValues["bots"] = true;
			}

			if (ret == null)
				return this;
			else
				return ret;
		}

		private GameScreen UpdateSelection(bool positive, ref bool confirmPressed)
		{
			int delta = positive ? 1 : -1;
			string s = Component.GetSelectedComponent(components).ReturnString;

			if (!confirmPressed)
			{
				confirmPressed = true;

				if (s == "map")
				{
					selectedMap = selectedMap + delta;
					if (selectedMap < 0)
						selectedMap = maps.Count - 1;
					else if (selectedMap >= maps.Count)
						selectedMap = 0;
				}
				else if (s == "time")
					timeLimit = timeLimit + delta > 10 ? 10 : (timeLimit + delta < 0 ? 0 : timeLimit + delta);
				else if (s == "lives")
					lives = lives + delta > 10 ? 10 : (lives + delta < 1 ? 1 : lives + delta);
				else if (s == "death" || s == "ranged" || s == "traps" || s == "bots")
				{
					((CheckBox)Component.GetSelectedComponent(components)).Checked = !((CheckBox)Component.GetSelectedComponent(components)).Checked;
					checkValues[s] = ((CheckBox)Component.GetSelectedComponent(components)).Checked;
				}
				else if (s == "start")
				{
					switch (prevScreen.Mode)
					{
						// Pass parameters eventually
						case -1:
							return new FreeForAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 0:
							return new FreeForAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 1:
							return new TeamDeathmatch(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
						case 2:
							return new OneVsAll(prevScreen.Colors, Map.MapKeys[maps[selectedMap]], gravity, timeLimit, lives, limbStrength, checkValues["death"], checkValues["traps"], checkValues["ranged"], checkValues["bots"]);
					}
				}
			}

			if (s == "grav")
				gravity = gravity + 0.04f * delta > 12 ? 12 : (gravity + 0.04f * delta < 0 ? 0 : gravity + 0.04f * delta);
			else if (s == "limb")
				limbStrength = limbStrength + 0.01f * delta > 1 ? 1 : (limbStrength + 0.01f * delta < 0 ? 0 : limbStrength + 0.01f * delta);

			return this;
		}

		public GameScreen GoBack()
		{
			return prevScreen;
		}

		public void Draw(SpriteBatch sb)
		{
			sb.Draw(maps[selectedMap], new Rectangle(0, 0, 1920, 1080), Color.White);
			Color c = new Color(255, 255, 255, 200);
			sb.Draw(MainGame.tex_blank, new Rectangle(600, 32, 723, 1016), c);

			sb.DrawString(MainGame.fnt_bigFont, "Game Settings", new Vector2(782, 100), Color.Black);

			sb.DrawString(MainGame.fnt_midFont, "Arena: ", new Vector2(680, 220), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, Map.MapKeys[maps[selectedMap]], new Vector2(970, 220), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, "Gravity:", new Vector2(680, 300), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, gravity.ToString(), new Vector2(970, 300), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, "Limb strength:", new Vector2(680, 380), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, ((int)(limbStrength * 100)).ToString() + "%", new Vector2(970, 380), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, "Time limit:", new Vector2(680, 460), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, timeLimit.ToString(), new Vector2(970, 460), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, "Lives:", new Vector2(680, 540), Color.Black);
			sb.DrawString(MainGame.fnt_midFont, lives.ToString(), new Vector2(970, 540), Color.Black);
			foreach (Component comp in components)
				comp.Draw(sb);
		}
	}
}
