using System;
using System.Collections.Generic;
using System.Linq;

namespace ALang
{
    public abstract class TreeElement
    {
        public int Line { get; set; } //TODO: initialize in parser

        public virtual bool IsCompileTime
        {
            get { return false; }
        }

        public virtual int RequiredSpaceInLocals { get; } = 0;

        public abstract void GenerateInstructions(Generator generator);

        public virtual void PrepareBeforeGenerate()
        {
        }

        public virtual void PrepareStatement(Generator generator)
        {
        }

        public virtual void CleanUpStatement(Generator generator)
        {
        }

        public void PrepareBeforeGenerateRecursive()
        {
            foreach (var child in m_children)
            {
                child.PrepareBeforeGenerateRecursive();
            }

            PrepareBeforeGenerate();
        }

        public void PrepareStatementRecursive(Generator generator)
        {
            foreach (var child in m_children)
            {
                child.PrepareStatementRecursive(generator);
            }

            PrepareStatement(generator);
        }

        public void CleanUpStatementRecursive(Generator generator)
        {
            foreach (var child in m_children)
            {
                child.CleanUpStatementRecursive(generator);
            }

            CleanUpStatement(generator);
        }

        public IList<TreeElement> Children()
        {
            return m_children;
        }

        public IList<T> Children<T>() where T : TreeElement
        {
            return m_children.Select(c => (T) c).ToList();
        }

        public T Child<T>(int index) where T : TreeElement
        {
            CheckChildrenCount(index);

            return (T) m_children[index];
        }

        public T Parent<T>() where T : TreeElement
        {
            return (T) m_parent;
        }

        public TreeElement Parent()
        {
            return m_parent;
        }

        public T RootParent<T>() where T : TreeElement
        {
            return m_parent == null ? (T) this : m_parent.RootParent<T>();
        }

        public TreeElement RootParent()
        {
            return m_parent == null ? this : m_parent.RootParent();
        }

        public void SetParent(TreeElement parent, int index = -1)
        {
            if (m_parent != null)
            {
                if (index == -1)
                {
                    index = m_parent.m_children.IndexOf(this);
                }
                m_parent.SetChild(m_parent.m_children.IndexOf(this), null);
            }

            m_parent = parent;

            if (m_parent == null)
                return;

            if (m_parent.ChildIndex(this) == -1)
            {
                m_parent.SetChild(index, this);
            }
        }

        public int ChildrenCount()
        {
            return m_children.Count;
        }

        public int ChildIndex(TreeElement child)
        {
            return m_children.IndexOf(child);
        }

        public int IndexInParent()
        {
            return m_parent.ChildIndex(this);
        }

        public TreeElement Child(int index)
        {
            CheckChildrenCount(index);

            return m_children[index];
        }

        public void AddChild(TreeElement child)
        {
            SetChild(m_children.Count, child);
        }

        public void SetChild(int index, TreeElement child)
        {
            if (index == -1)
            {
                index = m_children.Count;
            }

            CheckChildrenCount(index);

            m_children[index] = child;

            if (child != null && child.m_parent != this)
            {
                child.SetParent(this);
            }
        }

        public void RemoveChildren()
        {
            m_children.Clear();
        }


        protected List<TreeElement> m_children = new List<TreeElement>();
        protected TreeElement m_parent = null;

        private void CheckChildrenCount(int expected)
        {
            if (expected > (m_children.Count - 1))
            {
                for (int i = 0, iend = expected - (m_children.Count - 1); i < iend; ++i)
                {
                    m_children.Add(null);
                }
            }
        }
    }


    public class PrintCurrentValElement : TreeElement //TODO: remove it
    {
        public override void GenerateInstructions(Generator generator)
        {
            VarGet.IsGet = true;
            VarGet.GenerateInstructions(generator);

            generator.AddOp(GenCodes.Print, 1,
                ByteConverter.New().CastByte((sbyte) LanguageSymbols.DefTypesName.Index.Int32).Bytes);
        }

        public GetVariableElement VarGet
        {
            get { return m_varGet; }
            set
            {
                m_varGet = value;
                AddChild(m_varGet);
            }
        }

        private GetVariableElement m_varGet;
    }
}