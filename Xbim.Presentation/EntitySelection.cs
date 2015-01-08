using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Xbim.XbimExtensions.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xbim.Common.XbimExtensions;

namespace Xbim.Presentation
{
    /// <summary>
    /// Provides a container for entity selections capable of undo/redo operations and notification of changes.
    /// </summary>
    public class EntitySelection : INotifyCollectionChanged, IEnumerable<IPersistIfcEntity>
    {
        private List<SelectionEvent> _selectionLog;
        private XbimIPersistIfcEntityCollection<IPersistIfcEntity> _selection = new XbimIPersistIfcEntityCollection<IPersistIfcEntity>();
        private int position = -1;

        /// <summary>
        /// Initialises an empty selection;
        /// </summary>
        /// <param name="KeepLogging">Set to True to enable activity logging for undo/redo operations.</param>
        public EntitySelection(bool KeepLogging = false)
        {
            if (KeepLogging)
                _selectionLog = new List<SelectionEvent>();
        }

        public void Undo()
        {
            if (_selectionLog == null)
                return;
            if (position >= 0)
            {
                RollBack(_selectionLog[position]);
                position--;
            }
        }

        public void Redo()
        {
            if (_selectionLog == null)
                return;
            if (position < _selectionLog.Count - 1)
            { 
                position++;
                RollForward(_selectionLog[position]);
            }
        }

        private void RollBack(SelectionEvent e) 
        {
            switch (e.Action)
            {
                case Action.ADD:
                    RemoveRange(e.Entities);
                    break;
                case Action.REMOVE:
                    AddRange(e.Entities);
                    break;
                default:
                    break;
            }
        }

        private void RollForward(SelectionEvent e)
        {
            switch (e.Action)
            {
                case Action.ADD:
                    AddRange(e.Entities);
                    break;
                case Action.REMOVE:
                    RemoveRange(e.Entities);
                    break;
                default:
                    break;
            }
        }

        //add without logging
        private IEnumerable<IPersistIfcEntity> AddRange(IEnumerable<IPersistIfcEntity> entities)
        { 
            List<IPersistIfcEntity> check = new List<IPersistIfcEntity>();
            foreach (var item in entities) //check all for redundancy
            {
                if (!_selection.Contains(item))
                {
                    _selection.Add(item);
                    check.Add(item);
                }
            }
            
            OnCollectionChanged(NotifyCollectionChangedAction.Add, check);
            return check;
        }

        //remove without logging
        private IEnumerable<IPersistIfcEntity> RemoveRange(IEnumerable<IPersistIfcEntity> entities)
        {
            List<IPersistIfcEntity> check = new List<IPersistIfcEntity>();

            foreach (var item in entities) //check all for existance
            {
                if (_selection.Contains(item))
                {
                    check.Add(item);
                    _selection.Remove(item);
                }
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, check);
            return check;
        }

        public void Add(IPersistIfcEntity entity)
        {
            if (entity == null)
                return;
            Add(new IPersistIfcEntity[] { entity });
        }

        public void Add(IEnumerable<IPersistIfcEntity> entity)
        {
            IEnumerable<IPersistIfcEntity> check = AddRange(entity);
            if (_selectionLog == null)
                return;
            _selectionLog.Add(new SelectionEvent() { Action = Action.ADD, Entities = check });
            ResetLog();
        }

        public void Remove(IPersistIfcEntity entity)
        {
            Remove(new IPersistIfcEntity[] { entity });
        }

        public void Remove(IEnumerable<IPersistIfcEntity> entity)
        {
            if (entity == null)
                return;
            IEnumerable<IPersistIfcEntity> check = RemoveRange(entity);
            if (_selectionLog == null)
                return;
            _selectionLog.Add(new SelectionEvent() { Action = Action.REMOVE, Entities = check });
            ResetLog();
        }

        private void ResetLog()
        {
            if (position == _selectionLog.Count - 2) 
                position = _selectionLog.Count - 1; //normal transaction
            if (position < _selectionLog.Count - 2) //there were undo/redo operations and action inbetween must be discarded
            {
                _selectionLog.RemoveRange(position + 1, _selectionLog.Count - 2);
                position = _selectionLog.Count - 1;
            }
        }

        private enum Action
        {
            ADD,
            REMOVE
        }

        private struct SelectionEvent
        {
            public Action Action;
            public IEnumerable<IPersistIfcEntity> Entities;
        }


        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<IPersistIfcEntity> entities)
        {
            if (action != NotifyCollectionChangedAction.Add && action != NotifyCollectionChangedAction.Remove)
                throw new ArgumentException("Only Add and Remove operations are supported");
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, entities));
            }
        }

        public IEnumerator<IPersistIfcEntity> GetEnumerator()
        {
            return _selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        /// <summary>
        /// Toggles the selection state of an IPersistIfcEntity
        /// </summary>
        /// <param name="item">the IPersistIfcEntity to add or remove from the selection</param>
        /// <returns>true if added; false if removed</returns>
        internal bool Toggle(IPersistIfcEntity item)
        {
            if (_selection.Contains(item))
            {
                this.Remove(item);
                return false;
            }
            else
            {
                this.Add(item);
                return true;
            }
        }

        internal void Clear()
        {
            // to preserve undo capability
            //
            IPersistIfcEntity[] t = new IPersistIfcEntity[_selection.Count];
            _selection.CopyTo(t, 0);
            this.RemoveRange(t);
        }
    }
}
