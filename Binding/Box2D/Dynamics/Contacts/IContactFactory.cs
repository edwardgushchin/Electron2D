namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    internal interface IContactFactory
    {
        Contact Create(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB);

        void Destroy(Contact contact);
    }
}