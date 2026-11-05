from .Data import jobs

category_options = ["land_vehicles", "water_vehicles", "air_vehicles", "places", "bonus_jobs",
                    "adventure_time_dlc", "star_wars_dlc"]
categories = {option: [name for name, job in jobs.items() if job["category"] == option] for option in category_options}

raw_location_dict = list(jobs)
addendums = [f"{num}%" for num in range(1, 101)]

locations_percentages = {location: [f"{location} {addendum}" for addendum in addendums] for location in
                         raw_location_dict}
objectsanity_dict = {location: [f"{location}: {part}" for part in job["objects"]] for location, job in jobs.items()}
stagesanity_dict = {location: [f"{location} Stage {n}" for n in range(1, job["stages"] + 1)]
                    for location, job in jobs.items()}
bonustasksanity_dict = {location: [f"{location} Bonus Task {n}" for n in range(1, job["bonus_tasks"] + 1)]
                        for location, job in jobs.items()}

flat_locations_percentages = [location for _, percentages in locations_percentages.items() for location in percentages]
flat_objectsanity_dict = [part for _, parts in objectsanity_dict.items() for part in parts]
flat_stagesanity_dict = [stage for _, stages in stagesanity_dict.items() for stage in stages]
flat_bonustasksanity_dict = [task for _, tasks in bonustasksanity_dict.items() for task in tasks]

location_dict = (flat_locations_percentages + flat_objectsanity_dict + flat_stagesanity_dict
                 + flat_bonustasksanity_dict)
