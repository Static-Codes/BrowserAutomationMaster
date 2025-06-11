## Supported Selectors:

### Basic

- **ID:** `#main-content`
- **Class:** `.btn-primary`
- **Type / Tag:** `div`
- **Custom Element:** `my-custom-element`
- **Name Attribute:** `[name="elementName"]` (Note this is NOT the same as Tag)
- **XPath Selector:** `//form[@class='exampleClassName']//input[@type='text']`

### Advanced

- **Universal:** `*`
- **Attribute (Presence):** `[href]`
- **Attribute (Value):** `[target=_blank]`
- **Attribute (Quoted Value):** `[title='A simple title']`
- **Attribute (Complex Quoted Value):** `[data-value="some complex 'value' with quotes"]`
- **Pseudo-class:** `:hover`
- **Pseudo-class (with arguments):** `:nth-child(2n+1)`
- **Pseudo-class (with complex arguments):** `:not(.visible, #main)`
- **Pseudo-element:** `::before`

### Limitations

- For complex/nested selectors like `:not(div > .item)`, it will capture `div > .item` as a single string but will not parse further.
- This handles attributes with quoted strings quite well, but it doesn't fully support **all** CSS escaping rules Example: `"id="foo.bar""` would require the selector `"#foo\.bar"`.
- This is **not** full CSS/XPATH parsing and will not catch all invalid selectors, however most rules are currently supported.
- The CSS parsing currently doesn't support the following characters: ">", "+", "~", " ", etc.
