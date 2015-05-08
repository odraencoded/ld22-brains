/* Copyright (C) 2011 Leonardo Augusto Pereira
 * 
 * This file is part of Brains!!! Zombie Game
 * 
 * Brains!!! Zombie Game is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Brains!!! Zombie Game is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Brains!!! Zombie Game.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using PixelFomat = System.Drawing.Imaging.PixelFormat;
using System.Diagnostics;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Brains
{
    class Program
    {
        static bool Quit;

        static GameWindow epicWINdow;
        static AudioContext epicSfxContext;

        // Somewhat long variables
        static int leBrnTexName;
        static int leCopTexName;
        static int leZmbTexName;
        static int leShtTexName;
        static int leCtyTexName;
        static int leIcnTexName;
        static int leNmbTexName;
        static int leBadTextName;
        static int leWinTexName;
        static int leEndTexName;

        static Sfx pewPewSnd; // This sound is for bulletz
        static Sfx tumTumSnd; // This sound is for epic pwnage
        static Sfx timTimSnd; // This sound is for epic failures
        static Sfx chompChompSnd; // Omnomnomnomnomnomnomonomonomonom
        const int frameLimit = 60;

        static Random rnd;

        static void Main()
        {
            rnd = new Random();

            epicWINdow = new GameWindow(640, 360, GraphicsMode.Default, "BRAINS!!!");
            epicWINdow.Icon = new Icon("Graphics/brains.ico");
            epicWINdow.Visible = true;

            epicSfxContext = new AudioContext();

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.ClearColor(0, 0, 0, 1);

            leBrnTexName = LoadTexturr("Graphics/leBrn.png");
            leCopTexName = LoadTexturr("Graphics/leCop.png");
            leZmbTexName = LoadTexturr("Graphics/leZmb.png");
            leShtTexName = LoadTexturr("Graphics/leSht.png");
            leCtyTexName = LoadTexturr("Graphics/leCty.png");
            leIcnTexName = LoadTexturr("Graphics/leIcn.png");
            leNmbTexName = LoadTexturr("Graphics/leNmb.png");
            leBadTextName = LoadTexturr("Graphics/leBad.png");
            leWinTexName = LoadTexturr("Graphics/leWin.png");
            leEndTexName = LoadTexturr("Graphics/leEnd.png");

            pewPewSnd = Sfx.Load("Sounds/pewPew.wav");
            tumTumSnd = Sfx.Load("Sounds/tumTum.wav");
            timTimSnd = Sfx.Load("Sounds/timTim.wav");
            chompChompSnd = Sfx.Load("Sounds/chompChomp.wav");

            Quit = false;

            int score = 0;
            int level = 1;

            while (!Quit && PLAY())
            {
                while (GAEM(level, ref score))
                {
                    if (score < 0) score = 0;

                    if (level < 10)
                    {
                        WIN(level, score);
                        level++;
                    }
                    else
                    {
                        FINALE(level, score);
                        level = 0;
                    }
                }
                
                if (score < 0) score = 0;

                LOSE(level, score);

                level = 1;
                score = 0;
            }

            GL.DeleteTexture(leBrnTexName);
            GL.DeleteTexture(leCopTexName);
            GL.DeleteTexture(leZmbTexName);
            GL.DeleteTexture(leShtTexName);
            GL.DeleteTexture(leCtyTexName);
            GL.DeleteTexture(leIcnTexName);
            GL.DeleteTexture(leNmbTexName);

            GL.DeleteTexture(leBadTextName);
            GL.DeleteTexture(leWinTexName);
            GL.DeleteTexture(leEndTexName);

            pewPewSnd.Unload();
            tumTumSnd.Unload();
            timTimSnd.Unload();
            chompChompSnd.Unload();
        }

        static bool GAEM(int level, ref int score)
        {
            long freq = Stopwatch.Frequency;
            const int bulletSpd = 3;
            const int cityScale = 32;
            const int zmbStpdty = frameLimit;
            const int zmbSight = 48 * 48 + 48 * 48;
            
            int zmbHealth, zmbCount, zmbTeethPower, bulletDmg, bulletRate, totalBrains;
            int cityW, cityH;

            if (level == 0)
            {
                cityW = 17;
                cityH = 13;

                zmbHealth = 0;
                zmbCount = 0;
                zmbTeethPower = 0;
                bulletDmg = 0;
                bulletRate = 6;
                totalBrains = 100;
            }
            else
            {
                if (level < 4)
                {
                    cityW = 5;
                    cityH = 5;
                    zmbHealth = frameLimit * 2 + 1;
                    bulletDmg = frameLimit * 2;
                    bulletRate = 10;

                    zmbCount = 22 * level;
                    if (level == 3) zmbCount += 22;
                }
                else
                {
                    if (level < 7) zmbCount = 100 + 19 * (level - 3) + level;
                    else zmbCount = 183 + level + 9 * (level - 7);

                    if (level < 6) cityW = 9;
                    else cityW = 13;

                    if (level < 8) cityH = 5;
                    else cityH = 9;

                    zmbHealth = frameLimit * level / 3;
                    bulletDmg = frameLimit;
                    bulletRate = 11 - level / 2;
                }

                zmbTeethPower = level;
                totalBrains = frameLimit * (5 + level);
            }

            // Pseudo player class
            int nomedBrains = 0;
            int reload = 0;
            int px, py;
            int pimg = 0;

            int bspdx = 0;
            int bspdy = bulletSpd;

            List<Bullet> manyBullets = new List<Bullet>();
            List<Zombie> manyZombies = new List<Zombie>();

            bool[,] city = null;
            Zombie[] ctyzmbz = null;

            MaekCity(cityW, cityH, cityScale, zmbCount, zmbHealth, out city, out ctyzmbz);
            manyZombies.AddRange(ctyzmbz);

            px = cityW * cityScale / 2;
            py = cityH * cityScale / 2;

            while (nomedBrains < totalBrains)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);

                long frameStart = Stopwatch.GetTimestamp();

                epicWINdow.ProcessEvents();

                if (!epicWINdow.Visible || epicWINdow.Keyboard[Key.Escape])
                {
                    Quit = true;
                    break;
                }

                int pmx = 0;
                int pmy = 0;
                bool FIRE = false;

                if (epicWINdow.Keyboard[Key.Up]) pmy -= 1;
                if (epicWINdow.Keyboard[Key.Down]) pmy += 1;
                if (epicWINdow.Keyboard[Key.Left]) pmx -= 1;
                if (epicWINdow.Keyboard[Key.Right]) pmx += 1;

                reload--;
                if (reload < 0 && (epicWINdow.Keyboard[Key.Space] || epicWINdow.Keyboard[Key.ControlLeft] || epicWINdow.Keyboard[Key.ShiftLeft]))
                {
                    reload = frameLimit / bulletRate;
                    FIRE = true;
                }

                MoevDoll(ref px, ref py, ref pimg, 5, pmx, pmy, cityScale, cityW, cityH, city);

                if (pmx != 0 || pmy != 0)
                {
                    bspdx = pmx * 3;
                    bspdy = pmy * 3;

                    if (pmy < 0) pimg = 3;
                    else if (pmx > 0) pimg = 2;
                    else if (pmx < 0) pimg = 1;
                    else pimg = 0;
                }

                if (FIRE)
                {
                    Bullet neoBullet = new Bullet();

                    neoBullet.x = px + rnd.Next(-1, 1);
                    neoBullet.y = py - 2 + rnd.Next(-1, 1);

                    neoBullet.sx = bspdx;
                    neoBullet.sy = bspdy;

                    manyBullets.Add(neoBullet);

                    pewPewSnd.Play();

                    if (level > 0) score--;
                }

                for (int i = 0; i < manyBullets.Count; )
                {
                    Bullet blt = manyBullets[i];

                    blt.img = rnd.Next(4);

                    blt.x += blt.sx;
                    blt.y += blt.sy;

                    bool remove = false;

                    if (blt.x < 0 || blt.x >= cityW * cityScale || blt.y < 0 || blt.y >= cityH * cityScale) remove = true;
                    else if (blt.x < px - 160 || blt.x > px + 160 || blt.y < py - 90 || blt.y > py + 90) remove = true;
                    else
                    {
                        int ctx = blt.x / cityScale;
                        int cty = blt.y / cityScale;

                        if (!city[ctx, cty])
                        {
                            remove = true;
                            timTimSnd.Play();
                        }
                    }
                    if (remove)
                    {
                        manyBullets.RemoveAt(i);
                    }
                    else i++;
                }

                bool nomed = false;

                for (int i = 0; i < manyZombies.Count; )
                {
                    Zombie zmb = manyZombies[i];

                    zmb.thinkCooldown--;
                    if (zmb.thinkCooldown <= 0)
                    {
                        int dist = (zmb.x - px) * (zmb.x - px) + (zmb.y - py) * (zmb.y - py);

                        if (dist < zmbSight)
                        {
                            zmb.tx = px + rnd.Next(-16, 17);
                            zmb.ty = py + rnd.Next(-16, 17);

                            zmb.thinkCooldown = zmbStpdty / 3;
                        }
                        else
                        {
                            zmb.tx = zmb.x + rnd.Next(-64, 65);
                            zmb.ty = zmb.y + rnd.Next(-64, 65);

                            zmb.thinkCooldown = zmbStpdty;
                        }

                        zmb.thinkCooldown += rnd.Next(-zmbStpdty / 4, zmbStpdty / 4);
                    }
                    zmb.rightFoot = !zmb.rightFoot;

                    if (zmb.rightFoot)
                    {
                        int zmx = 0;
                        int zmy = 0;

                        if (zmb.tx > zmb.x + 2) zmx = 1;
                        if (zmb.tx < zmb.x - 2) zmx = -1;
                        if (zmb.ty > zmb.y + 2) zmy = 1;
                        if (zmb.ty < zmb.y - 2) zmy = -1;

                        MoevDoll(ref zmb.x, ref zmb.y, ref zmb.img, 5, zmx, zmy, cityScale, cityW, cityH, city);
                    }
                    bool hit = false;

                    for (int j = 0; j < manyBullets.Count; j++)
                    {
                        Bullet blt = manyBullets[j];

                        if (blt.x >= zmb.x - 4 && blt.x <= zmb.x + 4 && blt.y >= zmb.y - 2 && blt.y <= zmb.y + 6)
                        {
                            hit = true;
                            zmb.hp -= bulletDmg;
                            manyBullets.RemoveAt(j);
                            break;
                        }
                    }

                    if (hit)
                    {
                        tumTumSnd.Play();
                        score += 2;
                    }
                    else if (zmb.hp < zmbHealth) zmb.hp++;

                    if (zmb.hp <= 0)
                    {
                        manyZombies.RemoveAt(i);
                        score += level * 5;
                    }
                    else
                    {
                        if (px >= zmb.x - 5 && px <= zmb.x + 5 && py >= zmb.y - 2 && py <= zmb.y + 5)
                        {
                            nomed = true;
                            nomedBrains += zmbTeethPower;
                        }
                        i++;
                    }
                }

                if (nomed)
                {
                    chompChompSnd.Play();
                }

                if (!nomed && nomedBrains > 0) nomedBrains--;

                int cx = px;
                int cy = py;

                if (cx - 160 / 2 < 0) cx = 160 / 2;
                if (cx + 160 / 2 > cityScale * cityW) cx = cityScale * cityW - 160 / 2;
                if (cy - 90 / 2 < 0) cy = 90 / 2;
                if (cy + 90 / 2 > cityScale * cityH) cy = cityScale * cityH - 90 / 2;

                SetCamera(cx, cy, 160, 90);

                // Draw city
                GL.BindTexture(TextureTarget.Texture2D, leCtyTexName);
                GL.Begin(BeginMode.Quads);
                for (int i = 0; i < cityW * cityH; i++)
                {
                    int ctyx = i % cityW;
                    int ctyy = i / cityW;

                    if (city[ctyx, ctyy]) // Road
                    {
                        if ((ctyx > 0 && city[ctyx - 1, ctyy]) && (ctyx < cityW - 1 && city[ctyx + 1, ctyy]) &&
                            !(ctyy > 0 && city[ctyx, ctyy - 1]) && !(ctyy < cityH - 1 && city[ctyx, ctyy + 1]))
                        {
                            if ((ctyy > 0 && city[ctyx - 1, ctyy - 1]) || (ctyy < cityH - 1 && city[ctyx - 1, ctyy + 1]))
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale, 0, 32, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 0, 48, 0, 0, 16, 128, 128);
                            }
                            else
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale, 0, 0, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 0, 16, 0, 0, 16, 128, 128);
                            }

                            if ((ctyy > 0 && city[ctyx + 1, ctyy - 1]) || (ctyy < cityH - 1 && city[ctyx + 1, ctyy + 1]))
                            {
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 16, 32, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 16, 32 + 16, 0, 0, 16, 128, 128);
                            }
                            else
                            {
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 16, 0, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 16, 16, 0, 0, 16, 128, 128);
                            }

                        }
                        else if (!(ctyx > 0 && city[ctyx - 1, ctyy]) && !(ctyx < cityW - 1 && city[ctyx + 1, ctyy]) &&
                            (ctyy > 0 && city[ctyx, ctyy - 1]) && (ctyy < cityH - 1 && city[ctyx, ctyy + 1]))
                        {
                            if ((ctyx > 0 && city[ctyx - 1, ctyy - 1]) || (ctyx < cityW - 1 && city[ctyx + 1, ctyy - 1]))
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale, 32, 32, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 32 + 16, 32, 0, 0, 16, 128, 128);
                            }
                            else
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale, 32, 0, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 32 + 16, 0, 0, 0, 16, 128, 128);
                            }

                            if ((ctyx > 0 && city[ctyx - 1, ctyy + 1]) || (ctyx < cityW - 1 && city[ctyx + 1, ctyy + 1]))
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 32, 32 + 16, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 32 + 16, 32 + 16, 0, 0, 16, 128, 128);
                            }
                            else
                            {
                                DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 32, 16, 0, 0, 16, 128, 128);
                                DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 32 + 16, 16, 0, 0, 16, 128, 128);
                            }
                        }
                        else
                        {
                            DrawSqr(ctyx * cityScale, ctyy * cityScale, 64, 0, 0, 0, 32, 128, 128);
                        }
                    }
                    else // Wall
                    {
                        if (city[ctyx - 1, ctyy])
                        {
                            if (city[ctyx, ctyy - 1]) DrawSqr(ctyx * cityScale, ctyy * cityScale, 0, 64, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale, ctyy * cityScale, 32, 64 + 16, 0, 0, 16, 128, 128);

                            if (city[ctyx, ctyy + 1]) DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 0, 64 + 16, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 32, 64 + 16, 0, 0, 16, 128, 128);
                        }
                        else
                        {
                            if (city[ctyx, ctyy - 1]) DrawSqr(ctyx * cityScale, ctyy * cityScale, 32, 64, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale, ctyy * cityScale, 0, 96, 0, 0, 16, 128, 128);

                            if (city[ctyx, ctyy + 1]) DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 32 + 16, 64 + 16, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale, ctyy * cityScale + 16, 0, 96 + 16, 0, 0, 16, 128, 128);
                        }

                        if (city[ctyx + 1, ctyy])
                        {
                            if (city[ctyx, ctyy - 1]) DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 16, 64, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 32 + 16, 64, 0, 0, 16, 128, 128);

                            if (city[ctyx, ctyy + 1]) DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 16, 64 + 16, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 32 + 16, 64, 0, 0, 16, 128, 128);
                        }
                        else
                        {
                            if (city[ctyx, ctyy - 1]) DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 32, 64, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale + 16, ctyy * cityScale, 16, 96, 0, 0, 16, 128, 128);

                            if (city[ctyx, ctyy + 1]) DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 32 + 16, 64 + 16, 0, 0, 16, 128, 128);
                            else DrawSqr(ctyx * cityScale + 16, ctyy * cityScale + 16, 16, 96 + 16, 0, 0, 16, 128, 128);
                        }
                    }
                }
                GL.End();

                // Draw srszmbzns
                GL.BindTexture(TextureTarget.Texture2D, leZmbTexName);
                GL.Begin(BeginMode.Quads);

                foreach (Zombie zombie in manyZombies)
                {
                    DrawSqr(zombie.x, zombie.y, zombie.img % 2 * 8, zombie.img / 2 * 8, 4, 7, 8, 16, 16);
                }
                GL.End();

                // Draw bltz
                GL.BindTexture(TextureTarget.Texture2D, leShtTexName);
                GL.Begin(BeginMode.Quads);
                foreach (Bullet bullet in manyBullets)
                {
                    DrawSqr(bullet.x, bullet.y, bullet.img % 2 * 8, bullet.img / 2 * 8, 4, 4, 8, 16, 16);
                }
                GL.End();

                // Draw Player
                GL.BindTexture(TextureTarget.Texture2D, leCopTexName);
                GL.Begin(BeginMode.Quads);
                DrawSqr(px, py, pimg % 2 * 8, pimg / 2 * 8, 4, 7, 8, 16, 16);
                GL.End();

                // Draw text stuff
                SetCamera(80, 45, 160, 90);

                GL.BindTexture(TextureTarget.Texture2D, leIcnTexName);
                GL.Begin(BeginMode.Quads);
                DrawSqr(2, 2, 0, 0, 0, 0, 16, 32, 16);
                DrawSqr(160 - 18, 2, 16, 0, 0, 0, 16, 32, 16);
                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, leNmbTexName);
                GL.Begin(BeginMode.Quads);

                if (nomedBrains > 0) DrawNmbs(18, 6, (int)(100.0f / totalBrains * (totalBrains - nomedBrains)), 3);
                else DrawNmbs(18, 6, 100, 3);

                DrawNmbs(160 - 20 - 6 * 3, 6, manyZombies.Count, 3);
                GL.End();

                // Epic swap
                epicWINdow.SwapBuffers();

                if (manyZombies.Count == 0 && level > 0) return true;

                // System needs his framely resource fix
                long frameEnd = Stopwatch.GetTimestamp();
                float frameTime = (float)(frameEnd - frameStart) / freq;

                if (frameTime < 1.0f / frameLimit) Thread.Sleep((int)((1.0f / frameLimit - frameTime) * 1000));
            }

            return false;
        }

        static bool PLAY()
        {
            SetCamera(80, 45, 160, 90);

            bool pressed = false;

            do
            {
                epicWINdow.ProcessEvents();

                if (epicWINdow.Keyboard[Key.Enter]) pressed = true;

                if (epicWINdow.Keyboard[Key.Escape] || !epicWINdow.Visible)
                {
                    Quit = true;
                    return false;
                }

                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.BindTexture(TextureTarget.Texture2D, leBrnTexName);

                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0, 0);
                GL.Vertex2(0, 0);

                GL.TexCoord2(1, 0);
                GL.Vertex2(160, 0);

                GL.TexCoord2(1, 1);
                GL.Vertex2(160, 90);

                GL.TexCoord2(0, 1);
                GL.Vertex2(0, 90);

                GL.End();

                epicWINdow.SwapBuffers();

                Thread.Sleep(50);
            } while (!(!epicWINdow.Keyboard[Key.Enter] && pressed));

            return true;
        }

        static void WIN(int level, int score)
        {
            SetCamera(80, 45, 160, 90);

            bool pressed = false;

            do
            {
                epicWINdow.ProcessEvents();

                if (epicWINdow.Keyboard[Key.Enter]) pressed = true;

                if (epicWINdow.Keyboard[Key.Escape] || !epicWINdow.Visible)
                {
                    Quit = true;
                    return;
                }

                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.BindTexture(TextureTarget.Texture2D, leWinTexName);

                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0, 0);
                GL.Vertex2(0, 0);

                GL.TexCoord2(1, 0);
                GL.Vertex2(160, 0);

                GL.TexCoord2(1, 1);
                GL.Vertex2(160, 90);

                GL.TexCoord2(0, 1);
                GL.Vertex2(0, 90);

                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, leNmbTexName);
                GL.Begin(BeginMode.Quads);
                DrawNmbs(65, 52, score, 0);
                DrawNmbs(72, 75, level, 0);
                GL.End();

                epicWINdow.SwapBuffers();

                Thread.Sleep(50);
            } while (!(!epicWINdow.Keyboard[Key.Enter] && pressed));
        }

        static void LOSE(int level, int score)
        {
            SetCamera(80, 45, 160, 90);

            bool pressed = false;

            do
            {
                epicWINdow.ProcessEvents();

                if (epicWINdow.Keyboard[Key.Enter]) pressed = true;

                if (epicWINdow.Keyboard[Key.Escape] || !epicWINdow.Visible)
                {
                    Quit = true;
                    return;
                }

                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.BindTexture(TextureTarget.Texture2D, leBadTextName);

                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0, 0);
                GL.Vertex2(0, 0);

                GL.TexCoord2(1, 0);
                GL.Vertex2(160, 0);

                GL.TexCoord2(1, 1);
                GL.Vertex2(160, 90);

                GL.TexCoord2(0, 1);
                GL.Vertex2(0, 90);

                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, leNmbTexName);
                GL.Begin(BeginMode.Quads);
                DrawNmbs(65, 52, score, 0);
                DrawNmbs(72, 75, level, 0);
                GL.End();

                epicWINdow.SwapBuffers();

                Thread.Sleep(50);
            } while (!(!epicWINdow.Keyboard[Key.Enter] && pressed));
        }

        static bool FINALE(int level, int score)
        {
            SetCamera(80, 45, 160, 90);

            bool pressed = false;
            do
            {
                epicWINdow.ProcessEvents();
                if (epicWINdow.Keyboard[Key.Enter]) pressed = true;

                if (epicWINdow.Keyboard[Key.Escape] || !epicWINdow.Visible)
                {
                    Quit = true;
                    return false;
                }

                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.BindTexture(TextureTarget.Texture2D, leEndTexName);

                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0, 0);
                GL.Vertex2(0, 0);

                GL.TexCoord2(1, 0);
                GL.Vertex2(160, 0);

                GL.TexCoord2(1, 1);
                GL.Vertex2(160, 90);

                GL.TexCoord2(0, 1);
                GL.Vertex2(0, 90);

                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, leNmbTexName);
                GL.Begin(BeginMode.Quads);
                DrawNmbs(65, 52, score, 0, 1);
                DrawNmbs(72, 75, level, 0, 1);
                GL.End();

                epicWINdow.SwapBuffers();

                Thread.Sleep(50);
            } while (!(!epicWINdow.Keyboard[Key.Enter] && pressed));

            return true;
        }

        static int LoadTexturr(string filename)
        {
            Bitmap source = new Bitmap(filename);

            int naem = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, naem);

            BitmapData sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFomat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, sourceData.Width, sourceData.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, sourceData.Scan0);

            source.UnlockBits(sourceData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            return naem;
        }

        static void DrawSqr(int x, int y, int ux, int uy, int offx, int offy, int side, int texW, int texH)
        {
            GL.TexCoord2((float)ux / texW, (float)uy / texH);
            GL.Vertex2(x - offx, y - offy);
            GL.TexCoord2((float)(ux + side) / texW, (float)uy / texH);
            GL.Vertex2(x - offx + side, y - offy);
            GL.TexCoord2((float)(ux + side) / texW, (float)(uy + side) / texH);
            GL.Vertex2(x - offx + side, y - offy + side);
            GL.TexCoord2((float)ux / texW, (float)(uy + side) / texH);
            GL.Vertex2(x - offx, y - offy + side);
        }

        static void DrawNmbs(int x, int y, int value, int minLength, int color = 0)
        {
            List<int> manyNmbs = new List<int>();

            do
            {
                manyNmbs.Add(value % 10);
                value /= 10;
            } while (value > 0);

            while (manyNmbs.Count < minLength) manyNmbs.Add(0);

            manyNmbs.Reverse();

            foreach (int nmb in manyNmbs)
            {
                DrawSqr(x, y, nmb * 8, 8*color, 0, 0, 8, 80, 16);
                x += 7;
            }
        }

        static void SetCamera(int cx, int cy, int w, int h)
        {
            GL.MatrixMode(MatrixMode.Projection);

            Matrix4 camera = Matrix4.LookAt(new Vector3(cx, cy, -1), new Vector3(cx, cy, 0), new Vector3(0, -1, 0))
                * Matrix4.CreateOrthographic(w, h, -10000, 10000);

            GL.LoadMatrix(ref camera);
        }

        static void MaekCity(int width, int height, int scale, int zmbCount, int zmbHealth, out bool[,] city, out Zombie[] zmbz)
        {
            city = new bool[width, height];
            zmbz = new Zombie[zmbCount];

            int livezmbz = 0;

            for (int i = 0; i < width * height; i++)
            {
                int cx = i % width;
                int cy = i / width;
                if (cx == 0 || cy == 0 || cx == width - 1 || cy == height - 1) city[cx, cy] = true;
                else if (cx % 2 == 0 || cy % 2 == 0) city[cx, cy] = true;
                else city[cx, cy] = false;
            }

            for (int i = 0; i < width * height; i++)
            {
                int cx = i % width;
                int cy = i / width;

                if (cx == 0 || cy == 0 || cx == width - 1 || cy == height - 1) continue;
                else if (cx % 2 == 0 && cy % 2 == 0)
                {
                    city[cx, cy] = true;

                    bool hasWall = !(city[cx + 1, cy] && city[cx - 1, cy] && city[cx, cy + 1] && city[cx, cy - 1]);

                    if (!hasWall)
                    {
                        int roll = rnd.Next(4);

                        if (cx == 2)
                        {
                            if (roll == 0) city[cx - 1, cy] = false;
                            if (roll == 1) city[cx + 1, cy] = false;
                        }
                        else
                        {
                            if (roll == 0 || roll == 1) city[cx + 1, cy] = false;
                        }
                        if (cy == 2)
                        {
                            if (roll == 2) city[cx, cy - 1] = false;
                            if (roll == 3) city[cx, cy + 1] = false;
                        }
                        else
                        {
                            if (roll == 2 || roll == 3) city[cx, cy + 1] = false;
                        }
                    }
                }
            }

            int zi = 0;
            int zmbw = width / 2 + 1;
            int zmbh = height / 2 + 1;

            int copx = zmbw / 2;
            int copy = zmbh / 2;

            while (livezmbz < zmbCount)
            {
                int zx = zi % zmbw;
                int zy = zi / zmbw;

                int iCanHazZmb = Math.Abs(zx - copx) + Math.Abs(zy -copy);

                if (iCanHazZmb > 0)
                {
                    Zombie neoZmb = zmbz[livezmbz] = new Zombie();

                    neoZmb.x = zx * scale * 2 + rnd.Next(scale);
                    neoZmb.y = zy * scale * 2 + rnd.Next(scale);

                    neoZmb.hp = zmbHealth;

                    livezmbz++;
                }

                zi = (zi + 1) % (zmbw * zmbh);   
            }
        }

        static void MoevDoll(ref int x, ref int y, ref int img, int dollH, int mx, int my, int pathScale, int pathW, int pathH, bool[,] paths)
        {
            int nx = x + mx;
            int ny = y + my;

            if (nx < 0) nx = 0;
            if (nx >= pathW * pathScale) nx = pathW * pathScale - 1;
            if (ny < dollH) ny = dollH;
            if (ny >= pathH * pathScale) ny = pathH * pathScale - 1;

            int ptx = x / pathScale;
            int pty = y / pathScale;
            int nptx = nx / pathScale;
            int npty = ny / pathScale;

            if (paths[nptx, npty])
            {
                x = nx;
                y = ny;
            }
            else if (paths[nptx, pty])
            {
                x = nx;
            }
            else if (paths[ptx, npty])
            {
                y = ny;
            }

            if (my < 0) img = 3;
            else if (mx > 0) img = 2;
            else if (mx < 0) img = 1;
            else img = 0;
        }
    }
}
