using UnityEngine;

namespace SITF
{
    /// Палитра интерфейса (см. документ «что нарисовать для UI» → «Цвета интерфейса»).
    public static class Palette
    {
        /// Чернильно-сливовый — подложки: инвентарь, меню, панели.
        public static readonly Color Ink = Hex(0x29, 0x28, 0x3F);
        /// Молочный с лиловым — текст и простые иконки на тёмном.
        public static readonly Color Milk = Hex(0xF2, 0xEA, 0xF0);
        /// Приглушённая лаванда — рамки, обычные кнопки, вторичные детали.
        public static readonly Color Lavender = Hex(0xB8, 0xA9, 0xC9);
        /// Мягкий абрикосовый — главный акцент: выбранный предмет, свет светлячков, главное действие. Использовать редко.
        public static readonly Color Apricot = Hex(0xED, 0xBD, 0x91);
        /// Пыльный голубой — магические подсказки и отдельные эффекты (воспоминание, туман у выхода).
        public static readonly Color Dusty = Hex(0xA6, 0xCB, 0xD8);

        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
