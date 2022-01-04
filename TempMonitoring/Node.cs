using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TempMonitoring
{
    public class Node : INotifyPropertyChanged
    {
        public List<string> LocHostNames { get; set; }
        public int? NodCode { get; set; }
        public int? PchCode { get; set; }
        public string Text { get; set; }
        public string Name { get; set; }
        public string ToolTip { get; set; }
        public ObservableCollection<Node> Children { get; set; }

        private SolidColorBrush _backColor;
        public SolidColorBrush BackColor 
        { 
            get 
            {
                return _backColor;
            }
            set 
            {
                _backColor = value;
                NotifyPropertyChanged("BackColor");
            }
        }

        public object TreeItemRef { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(Node comp)
        {
            if (this.LocHostNames.Count == comp.LocHostNames.Count &&
                this.Text == comp.Text &&
                this.Name == comp.Name &&
                this.ToolTip == comp.ToolTip){
                    for (int i = 0; i < this.LocHostNames.Count; i++)
                        if (this.LocHostNames[i] != comp.LocHostNames[i])
                            return false;
                    return true;
            }
            else
                return false;
        }
        public bool containsChild(Node comp)
        {
            foreach (Node child in Children)
                if (this.LocHostNames.Count == comp.LocHostNames.Count &&
                this.Text == comp.Text &&
                this.Name == comp.Name &&
                this.ToolTip == comp.ToolTip)
                {
                    for (int i = 0; i < this.LocHostNames.Count; i++)
                        if (this.LocHostNames[i] != comp.LocHostNames[i])
                            return false;
                    return true;
                }
            return false;
        }

        public int IndexOfChildByName(string compName)
        {
            if (Children == null)
                return -1;

            for (int i = 0; i < Children.Count; i++)
                if (Children[i].Name == compName)
                    return i;
            return -1;
        }

        public Node FindByName(string Name)
        {
            if (this.Name.Equals(Name))
                return this;
            else
            {
                foreach (Node child in Children)
                {
                    Node result = child.FindByName(Name);
                    if (result != null)
                        return result;
                }
                return null;
            }
        }
    }
}
