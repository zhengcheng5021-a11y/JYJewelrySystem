Component({
  properties: {
    label: { type: String, value: '' },
    value: { type: String, value: '' },
    placeholder: { type: String, value: '请选择' },
    disabled: { type: Boolean, value: false }
  },
  methods: {
    onTap() { if (!this.data.disabled) this.triggerEvent('tap') }
  }
})
