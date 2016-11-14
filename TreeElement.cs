using System;
using System.Collections.Generic;

public abstract class TreeElement
{
    public int Line;//TODO: initialize in parser
    public virtual bool IsCompileTime
    {
        get{ return false; }
    } 

    public abstract void GenLowLevel(Generator generator);
    public virtual void PrepareBeforeGenerate() {}

    public void PrepareTree()
    {
        foreach(var child in m_children)
        {
            child.PrepareTree();
        }

        PrepareBeforeGenerate();
    }

    public IList<TreeElement> Children()
    {
        return m_children;
    }
    public T Child<T>(int index) where T : TreeElement
    {
        CheckChildrenCount(index);

        return (T)m_children[index];
    }

    public T Parent<T>() where T : TreeElement
    {
        return (T)m_parent;
    }
    public TreeElement Parent()
    {
        return m_parent;
    }
    public void SetParent(TreeElement parent, int index = -1)
    {
        if(m_parent != null)
        {
            if(index == -1)
            {
                index = m_parent.m_children.IndexOf(this);
            }
            m_parent.SetChild(m_parent.m_children.IndexOf(this), null);
        }

        m_parent = parent;

        if(m_parent.ChildIndex(this) == -1)
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
        CheckChildrenCount(index);

        m_children[index] = child;

        if(child != null && child.m_parent != this)
        {
            child.SetParent(this);
        }
    }

    protected List<TreeElement> m_children = new List<TreeElement>();
    protected TreeElement m_parent = null;

    private void CheckChildrenCount(int expected)
    {
        if(expected > (m_children.Count-1))
        {
            for(int i = 0, iend = expected - (m_children.Count-1); i < iend; ++i)
            {
                m_children.Add(null);
            }
        }
    }
}


public class PrintCurrentValElement : TreeElement//TODO: remove it
{
    public override void GenLowLevel(Generator generator)
    {
        VarGet.IsGet = true;
        VarGet.GenLowLevel(generator);

        generator.AddOp(GenCodes.Print, 0, null);
    }

    public VarGetSetValElement VarGet;
}