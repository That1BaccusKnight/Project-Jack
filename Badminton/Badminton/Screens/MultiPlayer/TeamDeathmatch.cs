﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using FarseerPhysics.Dynamics;

using Badminton.Stick_Figures;
using Badminton.Screens.Menus;
using Badminton.Attacks;

namespace Badminton.Screens.MultiPlayer
{
	class TeamDeathmatch : GameScreen
	{
		World world;
		List<Wall> walls;

		StickFigure[] player;
		Vector2[] spawnPoints;
		TrapAmmo[] ammo;
		Texture2D background;

		PlayerValues[] info;

		bool timed;
		int millisLeft;
		int startPause;

		bool gameOver;
		List<int> winners;
		List<StickFigure> winSticks;

		private static Category[] Categories = new Category[] { Category.Cat1, Category.Cat2, Category.Cat1, Category.Cat2 };
		private static PlayerIndex[] Players = new PlayerIndex[] { PlayerIndex.One, PlayerIndex.Two, PlayerIndex.Three, PlayerIndex.Four };

		public TeamDeathmatch(Color[] colors, string mapString, float gravity, int minutes, int lives, float limbStrength, bool suddenDeath, bool traps, bool longRange, bool bots)
		{
			world = new World(new Vector2(0, gravity));

			object[] map = Map.LoadMap(world, mapString);
			background = (Texture2D)map[0];
			walls = (List<Wall>)map[1];
			spawnPoints = (Vector2[])map[2];
			Vector3[] ammoPoints = (Vector3[])map[3];
			ammo = new TrapAmmo[ammoPoints.Length];
			if (traps)
				for (int i = 0; i < ammoPoints.Length; i++)
					ammo[i] = new TrapAmmo(world, new Vector2(ammoPoints[i].X, ammoPoints[i].Y) * MainGame.PIXEL_TO_METER, (int)ammoPoints[i].Z);

			StickFigure.AllowTraps = traps;
			StickFigure.AllowLongRange = longRange;

			player = new StickFigure[bots ? 4 : colors.Length];
			this.info = new PlayerValues[bots ? 4 : colors.Length];

			for (int i = 0; i < colors.Length; i++)
				if (colors[i] != null)
				{
					player[i] = new LocalPlayer(world, spawnPoints[i] * MainGame.PIXEL_TO_METER, Categories[i], 1.5f, limbStrength, suddenDeath ? 0.001f : 1f, i % 2 == 1, colors[i], Players[i]);
					player[i].LockControl = true;
				}

			if (bots && colors.Length < 4)
			{
				for (int i = colors.Length; i < 4; i++)
				{
					player[i] = new BotPlayer(world, spawnPoints[i] * MainGame.PIXEL_TO_METER, Categories[i], 1.5f, limbStrength, suddenDeath ? 0.001f : 1f, i % 2 == 1, new Color(i * 60, i * 60, i * 60), Players[i], player);
					player[i].LockControl = true;
				}
			}

			for (int i = 0; i < info.Length; i++)
				info[i] = new PlayerValues(lives);

			timed = minutes > 0;
			millisLeft = (minutes == 0 ? -1 : minutes * 60000);
			startPause = 180;
			gameOver = false;
			winners = new List<int>();
			winSticks = new List<StickFigure>();
		}

		public GameScreen Update(GameTime gameTime)
		{
			startPause--;
			if (startPause == 0)
			{
				foreach (StickFigure s in player)
					if (s != null)
						s.LockControl = false;
			}

			for (int i = 0; i < player.Length; i++)
			{
				if (player[i] != null && info[i].HasLives())
				{
					player[i].Update();
					if (player[i].IsDead || player[i].Position.Y * MainGame.METER_TO_PIXEL > 1080)
					{
						if (info[i].RespawnTimer < 0)
							info[i].Kill();

						if (info[i].ShouldRespawn())
						{
							player[i].Destroy();
							if (info[i].HasLives())
								player[i] = player[i].Respawn();
							else
								player[i] = null;
							info[i].RespawnTimer--;
						}
						else
							info[i].RespawnTimer--;
					}
				}
			}

			// Update ammo
			foreach (TrapAmmo t in ammo)
				if (t != null)
					t.Update();

			// Endgame
			if (!gameOver)
			{
				if (timed && startPause < 0)
					millisLeft -= gameTime.ElapsedGameTime.Milliseconds;
				gameOver = GameIsOver(winners);
			}
			else
			{
				if (winSticks.Count == 0)
				{
					for (int i = 0; i < winners.Count; i++)
					{
						winSticks.Add(new StickFigure(world, new Vector2(600 + 80 * i, 500) * MainGame.PIXEL_TO_METER, Category.None, 3f, 1, 1, winners[i] % 2 == 1, player[winners[i]].Color));
						winSticks[i].Invulnerability = 0;
						winSticks[i].Stand();
					}
				}

				foreach (StickFigure s in winSticks)
				{
					s.Update();
					s.ApplyForce(world.Gravity * -1);
				}

				if ((Keyboard.GetState().IsKeyDown(Keys.Enter) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Start)))
					return GoBack();
			}

			world.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
			return this;
		}

		protected virtual bool GameIsOver(List<int> winners)
		{
			bool gameOver = true;
			winners.Clear();
			for (int i = 0; i <= 1; i++)
			{
				if (info[i].Lives > 0 || (info.Length > i + 2 ? info[i + 2].Lives > 0 : false))
				{
					winners.Add(i);
					if (info.Length > i + 2) winners.Add(i + 2);
					if (winners.Count > (info.Length > 2 ? 2 : 1))
					{
						gameOver = false;
						winners.Clear();
						break;
					}
				}
			}

			if (gameOver)
				return true;
			else if (timed && millisLeft <= 0)
			{
				gameOver = true;
				int lives1 = (info.Length > 2 ? (info[0].Lives + info[2].Lives) / 2 : info[0].Lives);
				int lives2 = (info.Length > 3 ? (info[1].Lives + info[3].Lives) / 2 : info[1].Lives);
				if (lives1 >= lives2)
				{
					winners.Add(0);
					if (info.Length > 2) winners.Add(2);
				}
				if (lives2 >= lives1)
				{
					winners.Add(1);
					if (info.Length > 3) winners.Add(3);
				}
			}

			return gameOver;
		}

		public void Draw(SpriteBatch sb)
		{
			// draw background
			sb.Draw(MainGame.tex_bg_castle, new Rectangle(0, 0, 1920, 1080), Color.White);

			// draw ammo
			foreach (TrapAmmo t in ammo)
				if (t != null)
					t.Draw(sb);

			// draw players
			for (int i = 0; i < player.Length; i++)
				if (player[i] != null)
					player[i].Draw(sb);

			// draw walls
			//			foreach (Wall w in walls)
			//				w.Draw(sb);

			// Draw HUD
			if (millisLeft >= 0)
				MainGame.DrawOutlineText(sb, MainGame.fnt_midFont, millisLeft / 60000 + ":" + (millisLeft % 60000 / 1000 < 10 ? "0" : "") + (millisLeft % 60000 / 1000 < 0 ? 0 : millisLeft % 60000 / 1000), Vector2.One, Color.White);
			for (int i = 0; i < info.Length; i++)
				info[i].Draw(sb, Vector2.UnitX * 450 + Vector2.UnitX * i * 300 + Vector2.UnitY * 940, player[i]);

			if (gameOver) // Exactly what it sounds like
				DrawGameOver(sb);

			if (startPause > 0)
				MainGame.DrawOutlineText(sb, MainGame.fnt_bigFont, "Ready...", new Vector2(900, 500), Color.Red);
			else if (startPause > -120)
				MainGame.DrawOutlineText(sb, MainGame.fnt_bigFont, "GO!", new Vector2(930, 500), Color.Green);
		}

		protected virtual void DrawGameOver(SpriteBatch sb)
		{
			sb.Draw(MainGame.tex_blank, new Rectangle(450, 200, 1020, 680), Color.White);
			sb.DrawString(MainGame.fnt_bigFont, "Game over!", new Vector2(860, 290), Color.Black);

			if (winners.Count <= (info.Length > 2 ? 2 : 1))
			{
				sb.DrawString(MainGame.fnt_midFont, "Winners: Player" + (winners.Count == 1 ? "" : "s") + " " + (winners.Count == 1 ? (winners[0] + 1) + "" : (winners[0] + 1) + ", " + (winners[1] + 1)) + "!", new Vector2(1000, 500), Color.Black);
				if (winSticks.Count > 0)
				{
					winSticks[0].Draw(sb);
					winSticks[1].Draw(sb);
				}
			}
			else
			{
				sb.DrawString(MainGame.fnt_midFont, "Tie reached!", new Vector2(1000, 450), Color.Black);
				string tiedPlayers = "Tied players: ";
				foreach (int i in winners)
					tiedPlayers += (tiedPlayers.Length == 14 ? "" : ", ") + (i + 1).ToString();
				sb.DrawString(MainGame.fnt_midFont, tiedPlayers, new Vector2(1000, 515), Color.Black);
				foreach (StickFigure s in winSticks)
					s.Draw(sb);
			}
			sb.DrawString(MainGame.fnt_basicFont, "Press start to continue", new Vector2(1200, 800), Color.Black);
		}

		public GameScreen GoBack()
		{
			return new MainMenu();
		}
	}
}
