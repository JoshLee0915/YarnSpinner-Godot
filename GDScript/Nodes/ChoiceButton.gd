extends Button
class_name ChoiceButton

signal selected(id)

export(int) var id: int = -1

func _ready():
	connect("pressed", self, "_onSelected")
	
func _onSelected():
	emit_signal("selected", id)
