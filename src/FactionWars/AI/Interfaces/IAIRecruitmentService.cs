namespace FactionWars.AI.Interfaces
{
    public interface IAIRecruitmentService
    {
        int TryAutoRecruit(string factionId, int maxTroopsToRecruit = 10);
    }
}
