using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SITF
{
    /// Любой объект, по которому можно кликнуть. Нужен Collider2D.
    [RequireComponent(typeof(Collider2D))]
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("На каком расстоянии по X героиня останавливается перед объектом")]
        public float interactDistance = 1.0f;

        /// Уникальный ключ объекта для сохранения: "Сцена/Имя".
        protected string Key => gameObject.scene.name + "/" + name;

        public virtual bool CanHover => true;
        public virtual CursorKind HoverCursor => CursorKind.Interact;
        public abstract void Interact(ItemId held);

        /// Порядок отрисовки — чтобы при наложении выбирался верхний объект.
        public int SortingOrder
        {
            get
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                return sr ? sr.sortingOrder : 0;
            }
        }
    }
}
