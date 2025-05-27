$(document).ready(function () {
    const $assignCourseModal = $('#assignCourseModal');
    const $modalProgramName = $('#modalProgramName');
    const $modalYearSemester = $('#modalYearSemester');
    const $availableCoursesList = $('#availableCoursesList');
    const $courseSearch = $('#courseSearch');
    const $confirmAssign = $('#confirmAssign');

    const $programSelect = $('#programSelect');
    const $yearLevelSelect = $('#yearLevelSelect');
    const $semesterSelect = $('#semesterSelect');
    const $academicYearSelect = $('#academicYearSelect');

    let selectedCoursesToAssign = new Set();
    let allAvailableCourses = [];

    // Dynamic academic years loading when program changes
    $programSelect.change(function () {
        var progCode = $(this).val();

        if (progCode) {
            $academicYearSelect.prop('disabled', true).html('<option>Loading...</option>');

            $.ajax({
                url: '/CurriculumCourse/GetAcademicYears',
                method: 'GET',
                data: { progCode: progCode },
                success: function (data) {
                    var options = '<option value="" disabled selected>Choose Academic Year...</option>';
                    if (data && data.length > 0) {
                        $.each(data, function (i, ay) {
                            options += '<option value="' + ay.AyCode + '">' + ay.DisplayText + '</option>';
                        });
                    } else {
                        options = '<option value="" disabled>No academic years found</option>';
                    }
                    $academicYearSelect.html(options);
                    $academicYearSelect.prop('disabled', false);
                },
                error: function () {
                    $academicYearSelect.html('<option value="" disabled>Error loading academic years</option>');
                    $academicYearSelect.prop('disabled', true);
                }
            });
        } else {
            $academicYearSelect.html('<option value="" disabled selected>Choose Academic Year...</option>');
            $academicYearSelect.prop('disabled', true);
        }
    });

    // Render available courses in the modal
    function renderAvailableCourses(courses) {
        $availableCoursesList.empty();

        if (courses.length === 0) {
            $availableCoursesList.append(
                '<tr><td colspan="8" class="text-center">No courses found.</td></tr>'
            );
            return;
        }

        courses.forEach(course => {
            const isChecked = selectedCoursesToAssign.has(course.code) ? 'checked' : '';
            const $row = $(`
                <tr>
                    <td><input type="checkbox" class="course-checkbox" data-code="${course.code}" ${isChecked}></td>
                    <td>${course.code}</td>
                    <td>${course.title}</td>
                    <td>${course.category || ''}</td>
                    <td>${course.prerequisite || 'None'}</td>
                    <td>${course.units !== undefined ? course.units : ''}</td>
                    <td>${course.lec !== undefined ? course.lec : ''}</td>
                    <td>${course.lab !== undefined ? course.lab : ''}</td>
                </tr>
            `);
            $availableCoursesList.append($row);
        });
    }

    // Fetch all courses for assigning
    function fetchCourses() {
        $availableCoursesList.empty();
        $availableCoursesList.append('<tr><td colspan="8" class="text-center">Loading courses...</td></tr>');

        return $.ajax({
            url: '/Course/GetAllCourses',
            method: 'GET',
            dataType: 'json'
        })
            .done(function (data) {
                allAvailableCourses = data.map(c => ({
                    code: c.code,
                    title: c.title,
                    category: c.category || '',
                    prerequisite: c.prerequisite || 'None',
                    units: c.units || '',
                    lec: c.lec || '',
                    lab: c.lab || ''
                }));
                renderAvailableCourses(allAvailableCourses);
            })
            .fail(function () {
                $availableCoursesList.empty();
                $availableCoursesList.append(
                    '<tr><td colspan="8" class="text-center text-danger">Failed to load courses.</td></tr>'
                );
            });
    }

    // Show modal setup
    $assignCourseModal.on('show.bs.modal', function () {
        const selectedProg = $programSelect.val();
        const selectedYear = $yearLevelSelect.val();
        const selectedSemester = $semesterSelect.val();

        if (!selectedProg || !selectedYear || !selectedSemester) {
            alert('Please select Program, Year Level, and Semester before assigning courses.');
            return false;
        }

        $modalProgramName.text(selectedProg);
        $modalYearSemester.text(`${selectedYear} - ${selectedSemester}`);

        selectedCoursesToAssign.clear();
        $courseSearch.val('');
        fetchCourses();
    });

    // Course search filter
    $courseSearch.on('input', function () {
        const query = $(this).val().toLowerCase();
        const filtered = allAvailableCourses.filter(
            c => c.code.toLowerCase().includes(query) || c.title.toLowerCase().includes(query)
        );
        renderAvailableCourses(filtered);
    });

    // Track selected courses in modal
    $availableCoursesList.on('change', '.course-checkbox', function () {
        const courseCode = $(this).data('code');
        if ($(this).is(':checked')) {
            selectedCoursesToAssign.add(courseCode);
        } else {
            selectedCoursesToAssign.delete(courseCode);
        }
    });

    // Confirm assigning selected courses
    $confirmAssign.on('click', function () {
        selectedCoursesToAssign.clear();
        $availableCoursesList.find('input.course-checkbox:checked').each(function () {
            selectedCoursesToAssign.add($(this).data('code'));
        });

        if (selectedCoursesToAssign.size === 0) {
            alert('Please select at least one course to assign.');
            return;
        }

        const selectedProg = $programSelect.val();
        const selectedYear = $yearLevelSelect.val();
        const selectedSemester = $semesterSelect.val();
        const selectedAY = $academicYearSelect.val();

        function generateCurCode(progCode, academicYear) {
            return `${progCode}(${academicYear})`;
        }

        const dataToSend = {
            curriculum: {
                curCode: generateCurCode(selectedProg, selectedAY),
                progCode: selectedProg,
                ayCode: selectedAY,
                createdAt: new Date().toISOString()
            },
            curriculumCourses: Array.from(selectedCoursesToAssign).map(courseCode => ({
                curCode: generateCurCode(selectedProg, selectedAY), // Same as above
                crsCode: courseCode,
                curYearLevel: selectedYear,
                curSemester: selectedSemester,
                createdAt: new Date().toISOString()
            }))
        };
        $.ajax({
            url: '/CurriculumCourse/AssignCourses',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dataToSend),
            success: function (response) {
                if (response.success) {
                    alert('✅ Courses assigned successfully!');
                    $assignCourseModal.modal('hide');
                } else {
                    alert('❌ Error: ' + (response.message || 'Failed to assign courses.'));
                }
            },
            error: function () {
                alert('⚠️ An error occurred while assigning courses. Please try again.');
            }
        });
    });
});
