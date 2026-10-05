using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    // History is measured in successful movement turns. Free shield actions are
    // included in the next movement snapshot, and do not consume history slots.
    public sealed class TurnUndoHistory
    {
        private sealed class Snapshot
        {
            public GridPosition Position;
            public FacingDirection Facing;
            public int Energy, MaximumEnergy, Turn;
            public long Score;
            public int[] Core;
            public GridPosition[] Shields;
            public EnemyManager.Snapshot Enemies;
            public Random.State RandomState;
        }

        private const int Capacity = 5;
        private readonly List<Snapshot> history = new(Capacity);
        private readonly PlayerState player;
        private readonly PlayerView playerView;
        private readonly PlayerEnergyState energy;
        private readonly CoreState core;
        private readonly CoreView coreView;
        private readonly ShieldState shields;
        private readonly ShieldView shieldView;
        private readonly ScoreState score;
        private readonly TurnManager turns;
        private readonly EnemyManager enemies;
        public int Count => history.Count;

        public TurnUndoHistory(PlayerState player, PlayerView playerView, PlayerEnergyState energy,
            CoreState core, CoreView coreView, ShieldState shields, ShieldView shieldView,
            ScoreState score, TurnManager turns, EnemyManager enemies)
        {
            this.player = player; this.playerView = playerView; this.energy = energy;
            this.core = core; this.coreView = coreView; this.shields = shields;
            this.shieldView = shieldView; this.score = score; this.turns = turns; this.enemies = enemies;
        }

        public void RecordBeforeMove()
        {
            if (history.Count == Capacity) history.RemoveAt(0);
            history.Add(new Snapshot
            {
                Position = player.Position, Facing = player.Facing,
                Energy = energy.Current, MaximumEnergy = energy.Total,
                Turn = turns.CurrentTurn, Score = score.Total, Core = core.Capture(),
                Shields = new List<GridPosition>(shields.Positions).ToArray(),
                Enemies = enemies.Capture(), RandomState = Random.state
            });
        }

        public bool TryUndo()
        {
            if (history.Count == 0) return false;
            Snapshot saved = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            player.Restore(saved.Position, saved.Facing);
            core.Restore(saved.Core);
            shields.Restore(saved.Shields);
            energy.Restore(saved.Energy, saved.MaximumEnergy);
            score.Restore(saved.Score);
            turns.Restore(saved.Turn);
            playerView.Apply(player);
            coreView.Apply(core, animate: false);
            shieldView.Apply(shields);
            enemies.Restore(saved.Enemies);
            Random.state = saved.RandomState;
            return true;
        }
    }
}
