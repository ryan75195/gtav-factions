using GTA;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void SetBlipColor(int blipHandle, DomainBlipColor color)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                // Convert our BlipColor enum to GTA's BlipColor
                blip.Color = ConvertBlipColor(color);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetBlipSprite(int blipHandle, int spriteId)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                blip.Sprite = (BlipSprite)spriteId;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetBlipName(int blipHandle, string name)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                blip.Name = name;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
    }
}
