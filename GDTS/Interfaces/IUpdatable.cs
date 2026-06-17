namespace clone
{
    public interface IUpdatable
    {
        void UpdateFrame(float dt);
    }

    public interface ILateUpdatable
    {
        void LateUpdateFrame(float dt);
    }

    public interface IFixedUpdatable
    {
        void FixedUpdateFrame(float dt);
    }
}