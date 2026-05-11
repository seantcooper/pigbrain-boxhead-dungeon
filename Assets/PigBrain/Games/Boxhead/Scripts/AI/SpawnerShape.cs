using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class SpawnerShape : MonoBehaviour
    {
        public float area => transform.localScale.x * transform.localScale.y * transform.localScale.z;
        public float flatArea => transform.localScale.x * transform.localScale.z;

        #region Gizmos
        void OnDrawGizmosSelected()
        {
            if (!gameObject.activeSelf) return;
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
        #endregion
    }
}