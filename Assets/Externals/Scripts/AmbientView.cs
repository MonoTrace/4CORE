using UnityEngine;

namespace FourCore
{
    public sealed class AmbientView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer particlePrefab;
        private SpriteRenderer[] dust;
        private EnemyManager encounter;
        private float age;
        public void Initialize(EnemyManager encounter)
        {
            this.encounter=encounter;dust=new SpriteRenderer[60];System.Random random=new(144);
            for(int i=0;i<dust.Length;i++)
            {
                SpriteRenderer sprite=Instantiate(particlePrefab,transform,false);dust[i]=sprite;sprite.sortingOrder=-20;
                sprite.transform.localPosition=new Vector3((float)random.NextDouble()*34f-17f,(float)random.NextDouble()*24f-12f,1f);
                float size=.025f+(float)random.NextDouble()*.03f;sprite.transform.localScale=Vector3.one*size;
                sprite.color=new Color(.4f,.55f,.7f,.1f+(float)random.NextDouble()*.15f);
            }
        }
        private void Update()
        {
            if(dust==null||encounter.Run.FreezeMoves>0)return;
            age+=Time.deltaTime;
            for(int i=0;i<dust.Length;i++)
            {
                Vector3 position=dust[i].transform.localPosition;
                position.x+=Time.deltaTime*(.035f+.002f*(i%7));position.y+=Mathf.Sin(age*.3f+i)*Time.deltaTime*.01f;
                if(position.x>17f)position.x=-17f;dust[i].transform.localPosition=position;
            }
        }
    }
}
