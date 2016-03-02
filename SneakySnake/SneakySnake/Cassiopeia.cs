using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace SneakySnake

{
    internal class Cassiopeia
    {
        private static readonly Spell Q = new Spell(SpellSlot.Q, 850);
        private static readonly Spell W = new Spell(SpellSlot.W, 850);
        private static readonly Spell E = new Spell(SpellSlot.E, 700);
        private static readonly Spell R = new Spell(SpellSlot.R, 800);
        private static Circle QCircle { get; set; }
        private static Circle ECircle { get; set; }
        private static Circle WCircle { get; set; }
        private static Circle RCircle { get; set; }
        private static Menu CassioMenu;
        private static Menu ComboCassioMenu;
        private static Menu HarassCassioMenu;
        private static Menu ClearCassioMenu;
        private static Menu LasthitCassioMenu;
        private static Menu DrawingsCassioMenu;
        private static Menu OtherCassioMenu;

        public static void OnLoad()
        {
            if (Player.Instance.ChampionName != "Cassiopeia")
            {
                return;
            }

            //SetSkillshots
            Q.SetSkillshot(600, 150, -1, Spell.SkillshotType.Circular);
            W.SetSkillshot(500, 250, 2500, Spell.SkillshotType.Circular);
            E.SetTargetted(200, -1);
            R.SetSkillshot(600, (int)(80*Math.PI/180), -1, Spell.SkillshotType.Cone);
            
            //Menu
            BuildMenu(); 

            //Circles
            QCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = Q.Range
            };
            ECircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = E.Range
            };
            WCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = W.Range
            };
            RCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = R.Range
            };

            //Events
            Drawing.OnDraw += OnDraw;
            Game.OnTick += OnTick;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Interrupter.OnInterruptableSpell += InterrupterOnOnInterruptableSpell;
            Spellbook.OnCastSpell += SpellbookOnOnCastSpell;
        }

        private static void SpellbookOnOnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot != SpellSlot.R || !ComboCassioMenu["Snake.combo.keyr"].Cast<KeyBind>().CurrentValue)
            {
                return;
            }

            args.Process = false;
        }

        private static void InterrupterOnOnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (!R.Instance.IsReady ||
                interruptableSpellEventArgs.DangerLevel < (DangerLevel)OtherCassioMenu["Snake.other.interrupterlevel"].Cast<Slider>().CurrentValue ||
                !OtherCassioMenu["Snake.other.interrupter"].Cast<CheckBox>().CurrentValue || !sender.IsFacing(Player.Instance))
            {
                return;
            }

            R.Cast(sender);
        }

        private static void LastHitting()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                return;
            }

            if (Player.Instance.ManaPercent < LasthitCassioMenu["Snake.lasthit.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && LasthitCassioMenu["Snake.lasthit.q"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, Q.Range)
                        .Where(h => !h.HasBuffOfType(BuffType.Poison))
                        .ToList();
                var farmLocation = GetCircularFarmLocation(minions.ToArray(), Q);
                if (farmLocation.MinionsHit >= 3)
                {
                    Player.CastSpell(Q.Slot, farmLocation.Position);
                }
            }

            if (W.Instance.IsReady && LasthitCassioMenu["Snake.lasthit.w"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, W.Range)
                        .Where(h => !h.HasBuffOfType(BuffType.Poison))
                        .ToList();

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), W);
                if (farmLocation.MinionsHit >= 3)
                {
                    Player.CastSpell(W.Slot, farmLocation.Position);
                }
            }

            if (E.Instance.IsReady && LasthitCassioMenu["Snake.lasthit.e"].Cast<CheckBox>().CurrentValue)
            {
                var target = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,Player.Instance.ServerPosition, E.Range)
                    .Where(
                        h =>
                            h.HasBuffOfType(BuffType.Poison) &&
                            h.Health < GetSpellDamage(SpellSlot.Q, h) &&
                            GetPoisonBuffEndTime(h) < Game.Time + E.Delay && h.HasBuffOfType(BuffType.Poison) &&
                            h.IsTargetable && Prediction.Health.GetPrediction(h, (int)E.Delay) > 0)
                    .OrderBy(h => h.Health)
                    .FirstOrDefault();

                if (target != null)
                {
                    Player.CastSpell(E.Slot, target);
                }
            }
        }

        private static void Combo()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return;
            }

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (Q.Instance.IsReady && ComboCassioMenu["Snake.combo.useq"].Cast<CheckBox>().CurrentValue)
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChancePercent >= ComboCassioMenu["Snake.combo.hitchanceq"].Cast<Slider>().CurrentValue)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
            if (W.Instance.IsReady && ComboCassioMenu["Snake.combo.usew"].Cast<CheckBox>().CurrentValue)
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChancePercent >= ComboCassioMenu["Snake.combo.hitchancew"].Cast<Slider>().CurrentValue)
                {
                    W.Cast(pred.CastPosition);
                }
            }

            if (ComboCassioMenu["Snake.combo.usee"].Cast<CheckBox>().CurrentValue)
            {
                if (GetPoisonBuffEndTime(target) < Game.Time + E.Delay && target.HasBuffOfType(BuffType.Poison))
                {
                    E.Cast(target);
                }
                else if (Player.Instance.ServerPosition.Distance(target) > E.Range ||
                         GetPoisonBuffEndTime(target) >= Game.Time + E.Delay &&
                            ComboCassioMenu["Snake.combo.changete"].Cast<CheckBox>().CurrentValue)
                {
                    var targetE = EntityManager.Heroes.Enemies.Where(h =>
                                h.IsValid && !h.IsInvulnerable &&
                                h.ServerPosition.Distance(Player.Instance.ServerPosition) < E.Range &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                target.HasBuffOfType(BuffType.Poison))
                        .OrderBy(h => h.Health)
                        .OrderBy(h => TargetSelector.GetPriority(h)).FirstOrDefault();
                    if (targetE != null)
                    {
                        E.Cast(targetE);
                    }
                }
            }

            if (R.Instance.IsReady && ComboCassioMenu["Snake.combo.user"].Cast<CheckBox>().CurrentValue)
            {
                if (ComboDmg(target) >= target.Health || (target.IsFacing(Player.Instance) && 
                    ComboDmg(target) + (GetSpellDamage(SpellSlot.E, target)*3) >= target.Health))
                {
                    var result = R.GetPrediction(target);
                    if (result.HitChancePercent >= ComboCassioMenu["Snake.combo.hitchancer"].Cast<Slider>().CurrentValue)
                    {
                        R.Cast(result.CastPosition);
                    }
                }
                else
                {
                    var heros = EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(R.Range)).ToArray();
                    var pred = R.GetPredictionAoe(heros).OrderBy(h => h.CollisionObjects.Count(x => x is AIHeroClient)).FirstOrDefault();
                    if (pred.CollisionObjects.Count(x => x is AIHeroClient) >=
                        ComboCassioMenu["Snake.combo.minhitr"].Cast<Slider>().CurrentValue)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                return;
            }
            if (Player.Instance.ManaPercent < HarassCassioMenu["Snake.harass.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (Q.Instance.IsReady && HarassCassioMenu["Snake.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
            }

            if (W.Instance.IsReady && HarassCassioMenu["Snake.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    W.Cast(pred.CastPosition);
                }
            }

            if (E.Instance.IsReady && HarassCassioMenu["Snake.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                if (GetPoisonBuffEndTime(target) < Game.Time + E.Delay &&
                    target.HasBuffOfType(BuffType.Poison))
                {
                    Player.Instance.Spellbook.CastSpell(E.Slot, target);
                }
                else if (Player.Instance.ServerPosition.Distance(target) > E.Range ||
                         GetPoisonBuffEndTime(target) >= Game.Time + E.Delay)
                {
                    var targetE = ObjectManager.Get<AIHeroClient>()
                        .Where(
                            h =>
                                h.IsValid && !h.IsInvulnerable && h.IsEnemy &&
                                h.ServerPosition.Distance(Player.Instance.ServerPosition) < E.Range &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                target.HasBuffOfType(BuffType.Poison))
                        .OrderBy(h => h.Health)
                        .OrderBy(h => TargetSelector.GetPriority(h)).FirstOrDefault();
                    if (targetE != null)
                    {
                        E.Cast(targetE);
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                return;
            }
            if (Player.Instance.ManaPercent < ClearCassioMenu["Snake.jclear.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && ClearCassioMenu["Snake.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.ServerPosition, Q.Range)
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    Q.Cast(minion);
                }
            }

            if (W.Instance.IsReady && ClearCassioMenu["Snake.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.ServerPosition, W.Range)
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    W.Cast(minion);
                }
            }

            if (E.Instance.IsReady && ClearCassioMenu["Snake.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.ServerPosition, E.Range)
                        .Where(
                            h =>
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison))
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    E.Cast(minion);
                }
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                return;
            }
            if (Player.Instance.ManaPercent < ClearCassioMenu["Snake.clear.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && ClearCassioMenu["Snake.clear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, Q.Range);

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), Q);
                if (farmLocation.MinionsHit >= 3)
                {
                    Q.Cast(farmLocation.Position);
                }
            }

            if (W.Instance.IsReady && ClearCassioMenu["Snake.clear.w"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, W.Range).ToList();

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), W);
                if (farmLocation.MinionsHit >= 3)
                {
                    W.Cast(farmLocation.Position);
                }
            }

            if (E.Instance.IsReady && ClearCassioMenu["Snake.clear.e"].Cast<CheckBox>().CurrentValue)
            {
                if (ClearCassioMenu["Snake.clear.laste"].Cast<CheckBox>().CurrentValue)
                {
                    var target = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, E.Range)
                        .Where(
                            h =>
                                h.HasBuffOfType(BuffType.Poison) &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison) &&
                                h.IsTargetable &&
                                Prediction.Health.GetPrediction(h, E.Delay*2) <= GetSpellDamage(SpellSlot.E, h) &&
                                Prediction.Health.GetPrediction(h, E.Delay*2 + (int)Player.Instance.AttackCastDelay) > 0)
                        .OrderBy(h => h.Health)
                        .FirstOrDefault();

                    if (target != null)
                    {
                        E.Cast(target);
                    }
                }
                else
                {
                    var target = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition, E.Range)
                        .Where(
                            h =>
                                h.HasBuffOfType(BuffType.Poison) &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison) &&
                                h.IsTargetable &&
                                (Prediction.Health.GetPrediction(h, E.Delay*2) < GetSpellDamage(SpellSlot.E, h) ||
                                 Prediction.Health.GetPrediction(h, E.Delay*2) >=
                                 Damage.GetAutoAttackDamage(Player.Instance, h)))
                        .OrderBy(h => h.Health)
                        .FirstOrDefault();

                    if (target != null)
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        private static double GetSpellDamage(SpellSlot slot, Obj_AI_Base target)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {75, 115, 155, 195, 235}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.45;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float) dmg);
                }
                case SpellSlot.W:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {90, 135, 180, 225, 270}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.9;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
                case SpellSlot.E:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {55, 80, 105, 130, 155}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.55;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
                case SpellSlot.R:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {150, 250, 350}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.5;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
            }

            return 0;
        }

        private static float ComboDmg(Obj_AI_Base enemy)
        {
            var qDmg = Q.Instance.IsReady ? GetSpellDamage(SpellSlot.Q, enemy) : 0;
            var wDmg = W.Instance.IsReady ? GetSpellDamage(SpellSlot.W, enemy) : 0;
            var eDmg = E.Instance.IsReady ? GetSpellDamage(SpellSlot.E, enemy) : 0;
            var rDmg = R.Instance.IsReady ? GetSpellDamage(SpellSlot.R, enemy) : 0;
            var dmg = 0d;

            dmg += qDmg;
            dmg += wDmg;
            dmg += eDmg*3;

            if (R.Instance.IsReady)
            {
                dmg += qDmg;
                dmg += eDmg;
                dmg += rDmg;
            }

            return (float) dmg;
        }

        private static void BuildMenu()
        {
            CassioMenu = MainMenu.AddMenu("Sneaky Snake", "Snake");

            ComboCassioMenu = CassioMenu.AddSubMenu("Combo", "snake.combo");
            ComboCassioMenu.AddGroupLabel("Combo");
            ComboCassioMenu.AddLabel("Q Settings");
            ComboCassioMenu.Add("Snake.combo.useq", new CheckBox("Use Q"));
            ComboCassioMenu.Add("Snake.combo.hitchanceq", new Slider("Q HitChance %", 75));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("W Settings");
            ComboCassioMenu.Add("Snake.combo.usew", new CheckBox("Use W"));
            ComboCassioMenu.Add("Snake.combo.hitchancew", new Slider("W HitChance %", 75));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("E Settings");
            ComboCassioMenu.Add("Snake.combo.usee", new CheckBox("Use E"));
            ComboCassioMenu.Add("Snake.combo.changete", new CheckBox("Change E Target if Main Target has no PoisonBuff"));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("R Settings");
            ComboCassioMenu.Add("Snake.combo.user", new CheckBox("Use R"));
            ComboCassioMenu.Add("Snake.combo.hitchancer", new Slider("R HitChance %", 75));
            ComboCassioMenu.Add("Snake.combo.minhitr", new Slider("MinimumHit by R", 3, 1, 5));
            ComboCassioMenu.Add("Snake.combo.keyr", new KeyBind("Assisted Ult", false, KeyBind.BindTypes.HoldActive, 'R'));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("Other Settings");
            ComboCassioMenu.Add("Snake.combo.disableaa", new CheckBox("Disable AA while Casting E"));

            HarassCassioMenu = CassioMenu.AddSubMenu("Harass", "snake.harass");
            HarassCassioMenu.AddGroupLabel("Harass");
            HarassCassioMenu.Add("Snake.harass.q", new CheckBox("Use Q"));
            HarassCassioMenu.Add("Snake.harass.w", new CheckBox("Use W"));
            HarassCassioMenu.Add("Snake.harass.e", new CheckBox("Use E"));
            HarassCassioMenu.Add("Snake.harass.mana", new Slider("Minimum Mana%", 30));

            ClearCassioMenu = CassioMenu.AddSubMenu("MinionClear", "snake.clear");
            ClearCassioMenu.AddGroupLabel("LaneClear");
            ClearCassioMenu.Add("Snake.clear.q", new CheckBox("Use Q"));
            ClearCassioMenu.Add("Snake.clear.w", new CheckBox("Use W"));
            ClearCassioMenu.Add("Snake.clear.e", new CheckBox("Use E"));
            ClearCassioMenu.Add("Snake.clear.laste", new CheckBox("Only LastHit with E", false));
            ClearCassioMenu.Add("Snake.clear.mana", new Slider("Minimum Mana%", 30));

            ClearCassioMenu.AddSeparator();
            ClearCassioMenu.AddGroupLabel("JungleClear");
            ClearCassioMenu.Add("Snake.jclear.q", new CheckBox("Use Q"));
            ClearCassioMenu.Add("Snake.jclear.w", new CheckBox("Use W"));
            ClearCassioMenu.Add("Snake.jclear.e", new CheckBox("Use E"));
            ClearCassioMenu.Add("Snake.jclear.mana", new Slider("Minimum Mana%", 30));

            LasthitCassioMenu = CassioMenu.AddSubMenu("LastHit", "snake.lasthit");
            LasthitCassioMenu.AddGroupLabel("LastHit");
            LasthitCassioMenu.Add("Snake.lasthit.q", new CheckBox("Use Q", false));
            LasthitCassioMenu.Add("Snake.lasthit.W", new CheckBox("Use Q", false));
            LasthitCassioMenu.Add("Snake.lasthit.e", new CheckBox("Use E", false));
            LasthitCassioMenu.Add("Snake.lasthit.mana", new Slider("Minimum Mana%", 30));

            DrawingsCassioMenu = CassioMenu.AddSubMenu("Drawings", "snake.drawings");
            DrawingsCassioMenu.AddGroupLabel("Drawings");
            DrawingsCassioMenu.Add("Snake.draw.q", new CheckBox("Draw Q"));
            DrawingsCassioMenu.Add("Snake.draw.w", new CheckBox("Draw W"));
            DrawingsCassioMenu.Add("Snake.draw.e", new CheckBox("Draw E"));
            DrawingsCassioMenu.Add("Snake.draw.r", new CheckBox("Draw R"));

            OtherCassioMenu = CassioMenu.AddSubMenu("Other", "snake.other");
            OtherCassioMenu.AddGroupLabel("Other");
            OtherCassioMenu.Add("Snake.other.interrupter", new CheckBox("Use Interrupter (R)"));
            OtherCassioMenu.AddLabel("Minimum Interrupter DangerLevel");
            var level = OtherCassioMenu.Add("Snake.other.interrupterlevel", new Slider("High", 2, 0, 2));
            level.OnValueChange += (sender, args) =>
            {
                switch (args.NewValue)
                {
                    case 0:
                    {
                        level.DisplayName = "Low";
                        break;
                    }
                    case 1:
                    {
                        level.DisplayName = "Medium";
                        break;
                    }
                    case 2:
                    {
                        level.DisplayName = "High";
                        break;
                    }
                }
            };
        }

        private static void OnDraw(EventArgs args)
        {
            //Spell Ranges
            if (DrawingsCassioMenu["Snake.draw.q"].Cast<CheckBox>().CurrentValue)
            {
                QCircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["Snake.draw.e"].Cast<CheckBox>().CurrentValue)
            {
                ECircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["Snake.draw.w"].Cast<CheckBox>().CurrentValue)
            {
                WCircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["Snake.draw.r"].Cast<CheckBox>().CurrentValue)
            {
                RCircle.Draw(Player.Instance.Position);
            }
        }

        private static void OnTick(EventArgs args)
        {
            Combo();
            LastHitting();
            LaneClear();
            JungleClear();
            Harass();

            if (ComboCassioMenu["Snake.combo.keyr"].Cast<KeyBind>().CurrentValue)
            {
                CastAssistedUlt();
            }
        }

        private static void CastAssistedUlt()
        {
            if (!R.Instance.IsReady)
            {
                return;
            }

            var heros = EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(R.Range)).ToArray();
            if (!heros.Any())
            {
                return;
            }

            var pred = R.GetPredictionAoe(heros).OrderBy(h => h.CollisionObjects.Count(x => x is AIHeroClient)).FirstOrDefault();
            if (pred == null)
            {
                return;
            }

            if (pred.CollisionObjects.Count(x => x is AIHeroClient) >= ComboCassioMenu["Snake.combo.minhitr"].Cast<Slider>().CurrentValue)
            {
                Console.WriteLine("cast");
                R.Cast(pred.CastPosition);
            }
        }

        private static FarmLocation GetCircularFarmLocation(Obj_AI_Base[] units, Spell spell)
        {
            if (!units.Any() || units == null)
            {
                return new FarmLocation
                {
                    MinionsHit = 0,
                    Position = new Vector3()
                };
            }

            var farmlocation =
                Prediction.Position.PredictCircularMissileAoe(units, spell.Range, spell.Width, spell.Delay, spell.Speed)
                    .OrderBy(h => h.CollisionObjects.Count(x => x.IsMinion)).FirstOrDefault();

            return new FarmLocation
            {
                MinionsHit = farmlocation.CollisionObjects.Count(h => h.IsMinion),
                Position = farmlocation.CastPosition
            };
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo && !E.Instance.IsReady &&
                ComboCassioMenu["Snake.combo.disableaa"].Cast<CheckBox>().CurrentValue)
            {
                args.Process = false;
            }
            //braumbuff //lichbane
        }

        public static float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            var buffEndTime = target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Poison)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
            return buffEndTime;
        }
    }

    public class FarmLocation
    {
        public int MinionsHit { get; set; }
        public Vector3 Position { get; set; }
    }
}
