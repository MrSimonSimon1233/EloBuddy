using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace SneakySnake
{
    class Spell
    {
        public Spell(SpellSlot slot, float range = -1)
        {
            Slot = slot;
            _range = range;
        }
        public SpellSlot Slot { get; set; }
        public float Range
        {
            get
            {
                if (!IsChargingspell)
                {
                    return _range;
                }

                if (IsCharging)
                {
                    var difference = ChargedMaxRange - ChargedMinRange;
                    var percent = (Game.Time - ChargedStarted) / (ChargedDelay / 1000f);
                    return ChargedMinRange + (difference * percent);
                }

                return ChargedMaxRange;
            }
            set
            {
                _range = value;
            }
        }
        public Vector3 RangeCheckPosition
        {
            get
            {
                return _rangeCheckPosition.IsValid() ? _rangeCheckPosition : Player.Instance.ServerPosition;
            }

            set
            {
                _rangeCheckPosition = value;
            }
        }
        public Vector3 SourcePosition
        {
            get
            {
                return _sourcePosition.IsValid() ? _sourcePosition : Player.Instance.ServerPosition;
            }

            set
            {
                _sourcePosition = value;
            }
        }
        public int Width { get; set; }
        public int Delay { get; set; }
        public int Speed { get; set; }
        public SkillshotType SkillShotType { get; set; }
        public bool IsSkillshot{ get; set; }
        public bool IsChargingspell { get; set; }
        public float ChargedStarted{ get; set; }
        public string ChargedSpellName { get; set; }
        public int ChargedMinRange { get; set; }
        public int ChargedMaxRange { get; set; }
        public int ChargedDelay { get; set; }
        private float _range { get; set; }
        private Vector3 _rangeCheckPosition { get; set; }
        private Vector3 _sourcePosition { get; set; }
        public void SetSourcePosition(Vector3 sourcePos = new Vector3(), Vector3 rangeCheckPos = new Vector3())
        {
            SourcePosition = sourcePos;
            RangeCheckPosition = rangeCheckPos;
        }
        public bool IsInRange(Obj_AI_Base unit)
        {
            return RangeCheckPosition.Distance(unit.ServerPosition) <= Range;
        }
        public bool IsInRange(Vector3 position)
        {
            return RangeCheckPosition.Distance(position) <= Range;
        }
        public bool Cast()
        {
            return Player.Instance.Spellbook.CastSpell(Slot);
        }
        public bool Cast(Obj_AI_Base target)
        {
            return Player.Instance.Spellbook.CastSpell(Slot, target);
        }
        public bool Cast(Vector3 position)
        {
            return Player.Instance.Spellbook.CastSpell(Slot, position);
        }
        public bool Cast(Vector3 from, Vector3 to)
        {
            return Player.Instance.Spellbook.CastSpell(Slot, from, to);
        }
        public bool CastIfHitchanceEquals(AIHeroClient target, HitChance hitChance, int allowedCollisionCount = -1)
        {
            return Player.Instance.Spellbook.CastSpell(Slot, target, GetPrediction(target, allowedCollisionCount).HitChance >= hitChance);
        }
        public bool CastCharged(Vector3 position)
        {
            return Player.Instance.Spellbook.UpdateChargeableSpell(Slot, position, true);
        }
        public void StartCharging(Vector3 position)
        {
            if (!IsCharging)
            {
                Player.Instance.Spellbook.CastSpell(SpellSlot.Q, position, false);
                Player.Instance.Spellbook.UpdateChargeableSpell(Slot, position, false);
            }
        }
        public PredictionResult GetPrediction(AIHeroClient target, int allowedCollisionCount = -1)
        {
            if (IsSkillshot)
            {
                switch (SkillShotType)
                {
                    case SkillshotType.Linear:
                        return Prediction.Position.PredictLinearMissile(target, Range, Width, Delay, Speed, allowedCollisionCount, SourcePosition);
                    case SkillshotType.Circular:
                        return Prediction.Position.PredictCircularMissile(target, Range, Width, Delay, Speed, SourcePosition);
                    case SkillshotType.Cone:
                        return Prediction.Position.PredictConeSpell(target, Range, Width, Delay, Speed, SourcePosition);
                }
            }
            return null;
        }
        public PredictionResult[] GetPredictionAoe(AIHeroClient[] targets, int allowedCollisionCount = -1)
        {
            if (IsSkillshot)
            {
                switch (SkillShotType)
                {
                    case SkillshotType.Circular:
                        return Prediction.Position.PredictCircularMissileAoe(targets, Range, Width, Delay, Speed, SourcePosition);
                    case SkillshotType.Cone:
                        return Prediction.Position.PredictConeSpellAoe(targets, Range, Width, Delay, Speed, SourcePosition);
                }
            }
            return null;
        }
        public void SetSkillshot(int delay, int width, int speed, SkillshotType skillshotType, Vector3 sourcePosition = new Vector3(), Vector3 rangeCheckPosition = new Vector3())
        {
            Delay = delay;
            Width = width;
            Speed = speed;
            SkillShotType = skillshotType;
            SourcePosition = sourcePosition;
            RangeCheckPosition = rangeCheckPosition;
            IsSkillshot = true;
        }
        public void SetTargetted(int delay, int speed, Vector3 sourcePosition = new Vector3(), Vector3 rangeCheckPosition = new Vector3())
        {
            Delay = delay;
            Speed = speed;
            SourcePosition = sourcePosition;
            RangeCheckPosition = rangeCheckPosition;
            IsSkillshot = false;
        }
        public void SetCharged(string spellName, int minRange, int maxRange, int chargedDelay)
        {
            IsChargingspell = true;
            ChargedSpellName = spellName;
            ChargedMinRange = minRange;
            ChargedMaxRange = maxRange;
            ChargedDelay = chargedDelay;
            ChargedStarted = 0;

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }
        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name != ChargedSpellName && !sender.IsMe)
            {
                return;
            }

            ChargedStarted = Game.Time;
        }
        private void Spellbook_OnCastSpell(Spellbook spellbook, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot != Slot || !IsCharging)
            {
                return;
            }

            Cast(args.EndPosition);
        }
        public SpellDataInst Instance
        {
            get { return Player.GetSpell(Slot); }
        }
        public bool IsCharging
        {
            get { return Player.Instance.Spellbook.IsCharging && ChargedStarted + (ChargedDelay / 1000f) > Game.Time; }
        }

        public enum SkillshotType
        {
            Linear,
            Circular,
            Cone
        }
    }
}
