namespace Monster
{
    public abstract class State
    {
        protected MonsterController Monster;

        public void SetStateMachine(MonsterController monsterController)
        {
            Monster = monsterController;
        }

        public abstract void EnterState();

        public abstract void ExitState();
        
        public abstract void UpdateState();
    }
}