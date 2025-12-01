public interface IEnemyBehaviour
{
    void Init(BaseEnemy enemy);
    void OnEnter();   // appelé quand le composant devient actif/assigné
    void OnExit();    // appelé quand on remplace le comportement
    void Tick();      // appelé chaque Update par BaseEnemy
}